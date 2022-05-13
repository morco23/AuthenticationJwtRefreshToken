using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace MorCohen.Models.AuthenticateModels
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}
