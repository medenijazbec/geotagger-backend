using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs.Admin
{
    public class AdminUserUpdateDto
    {
        [Required] public string FirstName { get; set; } = null!;
        [Required] public string LastName { get; set; } = null!;
        [Required, EmailAddress] public string Email { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
    }
}
