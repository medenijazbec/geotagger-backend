using Microsoft.AspNetCore.Identity;
using geotagger_backend.DTOs;
using geotagger_backend.Models;

namespace geotagger_backend.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterDto dto);
        Task<LoginResult> LoginAsync(LoginDto dto);
        Task<IdentityResult> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto dto);
        Task<string?> ExternalLoginAsync(ExternalLoginDto dto);
        Task<string> GenerateJwtForUserAsync(ApplicationUser user);
        Task<LoginResult> RefreshTokenAsync(string refreshToken);

    }
}
