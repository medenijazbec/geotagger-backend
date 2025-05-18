using Microsoft.AspNetCore.Identity;

namespace geotagger_backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        //non-nullable with default values to avoid constructor warnings
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        //for the "Change profile picture" functionality
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}
