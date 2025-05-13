using Microsoft.AspNetCore.Identity;
using geotagger_backend.DTOs;

namespace geotagger_backend.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterDto dto);
        Task<string?> LoginAsync(LoginDto dto);
        Task<IdentityResult> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto dto);
        Task<string?> ExternalLoginAsync(ExternalLoginDto dto);

    }
}
