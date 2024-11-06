
using AonFreelancing.Contexts;
using AonFreelancing.Models;
using AonFreelancing.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AonFreelancing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<MainAppContext>(options => options.UseSqlite("Data Source=aon.db"));
            builder.Services.AddIdentity<User,ApplicationRole>()
                .AddEntityFrameworkStores<MainAppContext>()
                .AddDefaultTokenProviders();



            // Register JWT settings for dependency injection (bind it to the "JwtSettings" section of appsettings.json)
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

            // JWT Authentication configuration


            // Add Authentication (even if manually handling JWT validation)
            _ = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });




            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
