using System.ComponentModel.DataAnnotations;

namespace PulseCare.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Role { get; set; } // Will be either "Patient" or "Doctor"
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; } // Keeping it plain text for rapid development
    }
}