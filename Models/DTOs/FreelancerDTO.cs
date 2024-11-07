namespace AonFreelancing.Models.DTOs
{
    public class FreelancerDTO:UserDTO
    {

        public string Skills { get; set; }
    }

    public class FreelancerRequestDTO : UserDTO
    {
        public string Skills { get; set; }
    }

    public class FreelancerResponseDTO : UserResponseDTO { 
        public string? Skills { get; set; }
    }

    public class ProfileResponseDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string UserType { get; set; }
        public object CompanyName { get; set; }
        public string Skills { get; set; }
    }
}
