using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public class RegisterDto
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string Surname { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
