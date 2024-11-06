using AonFreelancing.Contexts;
using AonFreelancing.Models;
using AonFreelancing.Models.DTOs;
using AonFreelancing.Models.Requests;
using AonFreelancing.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AonFreelancing.Controllers.Mobile.v1
{
    [Route("api/mobile/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MainAppContext _mainAppContext;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly JwtSettings _jwtSettings;
        public AuthController(
            UserManager<User> userManager,
            MainAppContext mainAppContext,
            IConfiguration configuration,
            IOptions<JwtSettings> jwtSettings
            )
        {
            _userManager = userManager;
            _mainAppContext = mainAppContext;
            _configuration = configuration;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegRequest Req)
        {
            // Enhancement for identifying which user type is
            User u = new User();
            if (Req.UserType == Constants.USER_TYPE_FREELANCER)
            {
                // Register User
                u = new Freelancer
                {
                    Name = Req.Name,
                    UserName = Req.Username,
                    PhoneNumber = Req.PhoneNumber,
                    Skills = Req.Skills
                };
            }
            if (Req.UserType == Constants.USER_TYPE_CLIENT)
            {
                u = new Models.Client
                {
                    Name = Req.Name,
                    UserName = Req.Username,
                    PhoneNumber = Req.PhoneNumber,
                    CompanyName = Req.CompanyName
                };
            }


            var Result = await _userManager.CreateAsync(u, Req.Password);

            if (!Result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>()
                {
                    IsSuccess = false,
                    Results = null,
                    Errors = Result.Errors
                    .Select(e => new Error()
                    {
                        Code = e.Code,
                        Message = e.Description
                    })
                    .ToList()
                });

            }

            var createdUser = await _userManager.FindByNameAsync(Req.Username);
            if (createdUser == null)
            {
                return NotFound(new ApiResponse<object>()
                {
                    IsSuccess = false,
                    Results = null,
                    Errors = new List<Error>()
                    {
                        new Error()
                        {
                            Code = "NOT_FOUND",
                            Message = "User not found"
                        }
                    }
                });
            }

            UserResponseDTO? CreatedUserResponse;

            if (createdUser is Freelancer freelancer)
            {
                CreatedUserResponse = new FreelancerResponseDTO
                {
                    Id = createdUser.Id,
                    Name = createdUser.Name,
                    PhoneNumber = createdUser.PhoneNumber,
                    UserType = Constants.USER_TYPE_FREELANCER,
                    Skills = freelancer.Skills,
                };
            }
            else if (createdUser is Models.Client client)
            {
                // Created user is client
                CreatedUserResponse = new ClientResponseDTO
                {
                    Id = createdUser.Id,
                    Name = createdUser.Name,
                    PhoneNumber = createdUser.PhoneNumber,
                    UserType = Constants.USER_TYPE_CLIENT,
                    CompanyName = client.CompanyName,
                };
            }
            else
            {
                CreatedUserResponse = null;
            }


            var otp = OTPManager.GenerateOtp();
            var otpEntry = new Otp
            {
                OtpCode = otp,
                PhoneNumber = Req.PhoneNumber,
                IsUsed = false,
                ExpireDate = DateTime.UtcNow.AddMinutes(10), // Set expiration time as needed
                CreatedAt = DateTime.UtcNow
            };

            _mainAppContext.Otps.Add(otpEntry);
            await _mainAppContext.SaveChangesAsync();
            // TO-DO(Week 05 Task)
            // This should be enhanced using AppSetting 
            var accountSid = _configuration["Twilio:Sid"];
            var authToken = _configuration["Twilio:Token"];
            TwilioClient.Init(accountSid, authToken);

            var messageOptions = new CreateMessageOptions(
               new PhoneNumber(_configuration["Twilio:To"])); //To
            messageOptions.From = new PhoneNumber(_configuration["Twilio:From"]);
            messageOptions.ContentSid = _configuration["Twilio:ContentSid"];
            messageOptions.ContentVariables = "{\"1\":\"" + otp + "\"}";


            var message = MessageResource.Create(messageOptions);



            if (Req.UserType == Constants.USER_TYPE_FREELANCER)
            {


                return Ok(new ApiResponse<FreelancerResponseDTO>()
                {
                    IsSuccess = true,
                    Errors = [],
                    Results = (FreelancerResponseDTO)CreatedUserResponse,
                });


            }
            else if (Req.UserType == Constants.USER_TYPE_CLIENT)
            {


                return Ok(new ApiResponse<ClientResponseDTO>()
                {
                    IsSuccess = true,
                    Errors = [],
                    Results = (ClientResponseDTO)CreatedUserResponse,
                });
            }

            return Ok(new ApiResponse<object?>()
            {
                IsSuccess = false,
                Results = null,
                Errors = [
                    new Error(){
                        Code = StatusCodes.Status500InternalServerError.ToString(),
                        Message = "Something went wrong"
                    }],

            });







        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] AuthRequest Req)
        {
            var user = await _userManager.Users
                .Where(u => u.UserName == Req.UserName)
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();
            if (user != null && await _userManager.CheckPasswordAsync(user, Req.Password))
            {

                if (!await _userManager.IsPhoneNumberConfirmedAsync(user))
                {
                    return Unauthorized(new List<Error>() {
                   new Error(){
                       Code = StatusCodes.Status401Unauthorized.ToString(),
                       Message = "Verify Your Account First"
                   }
                });
                }

                var token = GenerateJwtToken(user);


                var freelanceruser = await _mainAppContext.Users
                    .OfType<Freelancer>()
                    .Where(u => u.Id == user.Id)
                    .FirstOrDefaultAsync();

                if (freelanceruser != null)
                {

                    FreelancerResponseDTO res = new FreelancerResponseDTO(
                        )
                    {
                        Id = freelanceruser.Id,
                        Name = freelanceruser.Name,
                        Username = freelanceruser.UserName,
                        PhoneNumber = freelanceruser.PhoneNumber,
                        IsPhoneNumberVerified = freelanceruser.PhoneNumberConfirmed,

                        UserType = Constants.USER_TYPE_FREELANCER,

                        Skills = freelanceruser.Skills,
                    };

                    return Ok(new ApiLoginResponse<FreelancerResponseDTO>
                    {
                        IsSuccess = true,
                        Errors = [],
                        AccessToken = token,
                        Results = res
                    });
                }


 // This is a client


                    var clientuser = await _mainAppContext.Users
                        .OfType<Models.Client>()
                        .Where(u => u.Id == user.Id)
                        .FirstOrDefaultAsync();

                    if (clientuser != null)
                    {

                        ClientResponseDTO res = new ClientResponseDTO()
                        {
                            Id = clientuser.Id,
                            Name = clientuser.Name,
                            Username = clientuser.UserName,
                            PhoneNumber = clientuser.PhoneNumber,
                            IsPhoneNumberVerified = clientuser.PhoneNumberConfirmed,
                            UserType = Constants.USER_TYPE_CLIENT,

                            CompanyName = clientuser.CompanyName,
                            Projects = _mainAppContext.Projects
                                .Where(p => p.ClientId == clientuser.Id)
                                .Select(p => new ProjectOutDTO()
                                {
                                    Id = p.Id,
                                    Title = p.Title,
                                    Description = p.Description,
                                    ClientId = p.ClientId,
                                })
                        };

                        return Ok(new ApiLoginResponse<ClientResponseDTO>
                        {
                            IsSuccess = true,
                            Errors = [],
                            AccessToken = token,
                            Results = res
                        });
                    }

                    

                
              
                  
                    return Unauthorized("Invalid user type.");
               



            }

            return Unauthorized(new List<Error>() {
                    new Error(){
                        Code = StatusCodes.Status401Unauthorized.ToString(),
                        Message = "UnAuthorized"
                    }
                });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAsync([FromBody] VerifyReq Req)
        {
            var user = await _userManager.Users.Where(x => x.PhoneNumber == Req.Phone).FirstOrDefaultAsync();
            if (user == null)
            {
                return BadRequest(new ApiResponse<string>()
                {
                    IsSuccess = false,
                    Results = null,
                    Errors = new List<Error>
                    {
                        new Error
                        {
                            Code = "UserNotFound",
                            Message = "User not found."
                        }
                    }
                });
            }

            if (!await _userManager.IsPhoneNumberConfirmedAsync(user))
            {
                var otp = await _mainAppContext.Otps
                    .Where(o => o.PhoneNumber == Req.Phone)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    return BadRequest(new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        Results = null,
                        Errors = new List<Error>
                        {
                            new Error
                            {
                                Code = "OtpNotFound",
                                Message = "Otp not found."
                            }
                        }
                    });
                }

                if (otp.IsUsed)
                {
                    return BadRequest(new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        Results = null,
                        Errors = new List<Error>
                        {
                            new Error
                            {
                                Code = "OtpUsed",
                                Message = "Otp already used."
                            }
                        }
                    });
                }



                if (!Req.Otp.Equals(otp.OtpCode))
                {
                    return BadRequest(new ApiResponse<string>()
                    {
                        IsSuccess = false,
                        Results = null,
                        Errors = new List<Error>
                        {
                            new Error
                            {
                                Code = "OtpNotMatch",
                                Message = "Otp not match."
                            }
                        }
                    });
                }

                otp.IsUsed = true;
                await _mainAppContext.SaveChangesAsync();

                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);

                return Ok(new ApiResponse<string>()
                {
                    IsSuccess = true,
                    Results = "Activated",
                    Errors = []
                });
            }
            else
            {
                return BadRequest(new ApiResponse<string>()
                {
                    IsSuccess = false,
                    Results = null,
                    Errors = new List<Error>
                        {
                            new Error
                            {
                                Code = "InvalidOTP",
                                Message = "The provided OTP is invalid or expired."
                            }
                        }
                });
            }














            return Unauthorized((new ApiResponse<string>()
            {
                IsSuccess = false,
                Results = null,
                Errors = new List<Error>() {
                    new Error(){
                        Code = StatusCodes.Status401Unauthorized.ToString(),
                        Message = "UnAuthorized"
                    }
                }
            }));
        }


        
       [HttpGet("{id} /Profile")]
 public async Task<IActionResult> GetProfileUser(long id)
 {

        var user = await _userManager.Users
            .Where(u => u.Id == id)
            .Select(u => new ProfileResponseDTO
            {
                Id = u.Id,
                Name = u.Name,
                Username = u.UserName,
                PhoneNumber = u.PhoneNumber,
                UserType = u.GetType().Name,
                CompanyName = u is Client ? ((Client)u).CompanyName : null,
                Skills = u is Freelancer ? ((Freelancer)u).Skills : null,
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return Unauthorized(new ApiResponse<ProfileResponseDTO>
            {
                IsSuccess = false,
                Results = null,
                Errors = new List<Error>() {
                    new Error(){
                        Code = StatusCodes.Status401Unauthorized.ToString(),
                        Message = "UnAuthorized"
                    }
                }
            });
        }

        return Ok(new ApiResponse<ProfileResponseDTO>
        {
            IsSuccess = true,
            Results = user,
            Errors = { }
        });




        // Helper function to generate JWT token
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiryInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }


}
