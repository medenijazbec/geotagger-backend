using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public class UpdateProfileDto
    {
        [StringLength(50, MinimumLength = 2)]
        public string? FirstName { get; set; }

        [StringLength(50, MinimumLength = 2)]
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? ProfilePictureUrl { get; set; }
    }
}
