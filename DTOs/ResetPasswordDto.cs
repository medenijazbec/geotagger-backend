using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public class ResetPasswordDto
    {
        //retrieved from the link in the reset email
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        //new password and confirmation
        [Required, StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
