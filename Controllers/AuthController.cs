using System.Security.Claims;
using System.Threading.Tasks;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using geotagger_backend.Helpers;
using System.Security.Cryptography;
using geotagger_backend.Data;
using DotNetEnv;
using static System.Net.WebRequestMethods;


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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly string _frontendBaseUrl;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _http;

        public AuthController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ApplicationDbContext db,
            IWebHostEnvironment env,               
            IHttpClientFactory http)              
        {
            _authService = authService;
            _signInManager = signInManager;
            _userManager = userManager;
            _config = config;
            _frontendBaseUrl = _config["FRONTEND_BASE_URL"] ?? "http://localhost:5173/";
            _db = db;
            _env = env;      
            _http = http;     
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
        [AllowAnonymous]
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


        // GET: api/Auth/confirm-email
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string uid, string tok)
        {
            var user = await _userManager.FindByIdAsync(uid);
            if (user == null) return NotFound();

            var res = await _userManager.ConfirmEmailAsync(user, tok);
            if (!res.Succeeded) return BadRequest(res.Errors);

            // Auto-login → JWT → redirect to frontend home
            var jwt = await _authService.GenerateJwtForUserAsync(user);
            var redirect = $"{_frontendBaseUrl}/home?emailConfirmed=1&token={Uri.EscapeDataString(jwt)}";
            return Redirect(redirect);
        }




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
            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
            {
                return Unauthorized(new
                {
                    Error = result.Error
                });
            }

            return Ok(new
            {
                Token = result.Token
            });
        }

        /// <summary>Rotate an existing refresh token into a new JWT + refresh.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            if (!result.Success)
                return Unauthorized(new { Error = result.Error });
            return Ok(new
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken
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


        [HttpGet("ExternalLogin")]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet("ExternalLoginCallback")]
        public async Task<IActionResult> ExternalLoginCallback(
    string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
                return Redirect($"{_frontendBaseUrl}/signin?externalLogin=error&message={Uri.EscapeDataString(remoteError)}");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return Redirect($"{_frontendBaseUrl}/signin?externalLogin=error&message=No external login info.");

            /* ── BASIC CLAIMS ─────────────────────────────────────────────── */
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                           ?? info.Principal.FindFirstValue("email");
            var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
            var familyName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
            var fullName = info.Principal.Identity?.Name ?? string.Empty;
            var avatarUrl = info.Principal.FindFirstValue("picture");

            if (string.IsNullOrWhiteSpace(email))
                return Redirect($"{_frontendBaseUrl}/signin?externalLogin=error&message=No email from provider.");

            /* ── SAFE FALL-BACKS ──────────────────────────────────────────── */
            var first = !string.IsNullOrWhiteSpace(givenName)
                        ? givenName
                        : fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                        ?? "GoogleUser";

            var last = !string.IsNullOrWhiteSpace(familyName)
                        ? familyName
                        : fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                        ?? "User";

            /* mirror the avatar once – or fall back to placeholder */
            avatarUrl = !string.IsNullOrWhiteSpace(avatarUrl)
                      ? await MirrorExternalAvatarAsync(avatarUrl)
                      : "/images/default_avatar.png";

            var displayName = info.ProviderDisplayName ?? info.LoginProvider ?? "External";

            /* ── TRY SIGN-IN BY EXTERNAL LOGIN KEY ────────────────────────── */
            var signIn = await _signInManager.ExternalLoginSignInAsync(
                             info.LoginProvider, info.ProviderKey, false);

            ApplicationUser user;

            if (signIn.Succeeded)
            {
                user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)!;

                await PatchProviderDisplayNameAsync(user, info, displayName);
                await user.UpdateProfileAsync(_userManager, first, last, avatarUrl);
            }
            else
            {
                /* ── FIRST-TIME WITH THIS PROVIDER ─────────────────────────── */
                user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        NormalizedUserName = email.ToUpperInvariant(),
                        Email = email,
                        NormalizedEmail = email.ToUpperInvariant(),
                        EmailConfirmed = true,
                        FirstName = first,
                        LastName = last,
                        ProfilePictureUrl = avatarUrl,
                        PasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(64)) // dummy pwd
                    };
                    await _userManager.CreateAsync(user);
                }
                else
                {
                    await user.EnsureCoreIdentityFieldsAsync(_userManager, email);
                    await user.UpdateProfileAsync(_userManager, first, last, avatarUrl);
                }

                var loginInfo = new UserLoginInfo(info.LoginProvider, info.ProviderKey, displayName);
                await _userManager.AddLoginAsync(user, loginInfo);
            }

            /* ── WALLET ROW + 100-POINT BONUS IF NEEDED ───────────────────── */
            await EnsureGeoUserAsync(user.Id);

            /* ── ISSUE JWT & REDIRECT BACK TO FRONTEND ────────────────────── */
            var jwt = await _authService.GenerateJwtForUserAsync(user);
            return Redirect($"{_frontendBaseUrl}/home?externalLogin=success&token={Uri.EscapeDataString(jwt)}");
        }


        private async Task EnsureGeoUserAsync(string userId)
        {
            var geo = await _db.GeoUsers.FindAsync(userId);

            if (geo == null)                           // first ever OAuth login
            {
                geo = new GeoUser
                {
                    UserId = userId,
                    GamePoints = 100
                };
                _db.GeoUsers.Add(geo);

                _db.GeoPointsTransactions.Add(new GeoPointsTransaction
                {
                    UserId = userId,
                    PointsDelta = 100,
                    Reason = PointsReason.registration_bonus
                });
            }
            else if (geo.GamePoints == 0)              // legacy rows with 0 pts
            {
                geo.GamePoints = 100;

                _db.GeoPointsTransactions.Add(new GeoPointsTransaction
                {
                    UserId = userId,
                    PointsDelta = 100,
                    Reason = PointsReason.registration_bonus
                });
            }

            await _db.SaveChangesAsync();
        }


        private async Task PatchProviderDisplayNameAsync(
        ApplicationUser user, ExternalLoginInfo info, string displayName)
        {
            var login = (await _userManager.GetLoginsAsync(user))
                        .First(l => l.LoginProvider == info.LoginProvider &&
                                    l.ProviderKey == info.ProviderKey);

            if (string.IsNullOrEmpty(login.ProviderDisplayName))
            {
                await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
                await _userManager.AddLoginAsync(
                        user,
                        new UserLoginInfo(login.LoginProvider, login.ProviderKey, displayName));
            }
        }


        private async Task<string> MirrorExternalAvatarAsync(string remoteUrl)
        {
            // Defensive – unknown or obviously bad URLs fall back to the placeholder
            if (string.IsNullOrWhiteSpace(remoteUrl) || remoteUrl.Length > 500)
                return "/images/default_avatar.png";

            var client = _http.CreateClient();          // _http is injected IHttpClientFactory
            using var resp = await client.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode ||
                !resp.Content.Headers.ContentType?.MediaType.StartsWith("image/") == true)
                return "/images/default_avatar.png";

            var ext = resp.Content.Headers.ContentType.MediaType switch
            {
                "image/png" => ".png",
                "image/gif" => ".gif",
                _ => ".jpg"
            };

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var avatarsDir = Path.Combine(_env.WebRootPath, "avatars");
            Directory.CreateDirectory(avatarsDir);
            var localPath = Path.Combine(avatarsDir, fileName);

            await using (var fs = System.IO.File.Create(localPath))
                await resp.Content.CopyToAsync(fs);

            // Public URL – Program.cs will expose /avatars/*
            return $"/avatars/{fileName}";
        }




    }
}