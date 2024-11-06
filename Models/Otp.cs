using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace AonFreelancing.Models
{
    //Entity
    [Table("Otps")]
    public class Otp
    {
        public int Id { get; set; }

        [Required]
        public string OtpCode { get; set; }


        [Required]
        public string PhoneNumber { get; set; }
        


        public bool IsUsed { get; set; }

        public DateTime ExpireDate { get; set; }

        public DateTime CreatedAt { get; set; }


    }
}
