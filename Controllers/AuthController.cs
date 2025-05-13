using System.Threading.Tasks;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// Controller responsible for user authentication and account management operations:
        /// registration, login, password reset, and (optional) email confirmation.
        /// </summary>
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user account with the provided credentials.
        /// </summary>
        /// <param name="dto">Data transfer object containing registration details (email, password, etc.).</param>
        /// <returns>
        /// <c>200 OK</c> with success message when registration succeeds.
        /// <c>400 Bad Request</c> with validation errors when the model state is invalid or registration fails.
        /// </returns>
        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // delegate to your AuthService
            var result = await _authService.RegisterAsync(dto);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // email confirm step comes later
            return Ok(new
            {
                Message = "Registration successful."
            });
        }

        /*
        // GET: api/Auth/confirm-email
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Email confirmed successfully." });
        }
        */

        /// <summary>
        /// Authenticates a user and issues a JWT token on successful login.
        /// </summary>
        /// <param name="dto">Data transfer object containing login credentials (email and password).</param>
        /// <returns>
        /// <c>200 OK</c> with JWT token when login succeeds.
        /// <c>401 Unauthorized</c> when credentials are invalid.
        /// </returns>
        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);
            if (token == null)
            {
                return Unauthorized(new
                {
                    Error = "Invalid email or password."
                });
            }
            return Ok(new
            {
                Token = token
            });
        }

        /// <summary>
        /// Initiates a password reset process by sending a reset link to the user's email.
        /// </summary>
        /// <param name="dto">Data transfer object containing the user's email address.</param>
        /// <returns>
        /// <c>200 OK</c> with a generic message regardless of whether the email exists.
        /// <c>400 Bad Request</c> if the request fails due to service errors.
        /// </returns>
        // POST: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok(new
            {
                Message = "If that email exists, a password reset link has been sent."
            });
        }

        /// <summary>
        /// Resets the user's password using the provided token and new password.
        /// </summary>
        /// <param name="dto">Data transfer object containing user ID, reset token, and new password.</param>
        /// <returns>
        /// <c>200 OK</c> when password is reset successfully.
        /// <c>400 Bad Request</c> if reset fails due to invalid token or other errors.</returns>
        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok(new
            {
                Message = "Password reset successful."
            });
        }

        /// POST: api/Auth/external-login
        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginDto dto)
        {
            var token = await _authService.ExternalLoginAsync(dto);
            if (token == null)
                return Unauthorized(new { Error = "External login failed." });

            return Ok(new { Token = token });
        }




    }
}