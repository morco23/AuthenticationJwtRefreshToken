using System.ComponentModel.DataAnnotations;

namespace MorCohen.Models.AuthenticateModels
{
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
