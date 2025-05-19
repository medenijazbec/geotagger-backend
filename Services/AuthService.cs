using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using Google.Apis.Auth;
using System.Text.Json;
using geotagger_backend.Data;
using static Org.BouncyCastle.Math.EC.ECCurve;
using geotagger_backend.Helpers;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace geotagger_backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _mailer;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext db, IConfiguration config, IEmailService mailer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _db = db;
            _config = config;
            _mailer = mailer;
        }

        /// <summary>
        /// Registers a new user with the specified credentials.
        /// </summary>
        /// <param name="dto">Data transfer object containing registration details.</param>
        /// <returns>
        /// An <see cref="IdentityResult"/> indicating success or failure.
        /// </returns>
        public async Task<IdentityResult> RegisterAsync(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                FirstName = dto.Name,
                LastName = dto.Surname,
                Email = dto.Email,
                UserName = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)                       // bail out on validation errors
                return result;

            // ── WALLET & LEDGER ────────────────────────────────────────────────
            var geoUser = new GeoUser           // starts with 100 pts
            {
                UserId = user.Id,
                GamePoints = 100
            };
            _db.GeoUsers.Add(geoUser);

            _db.GeoPointsTransactions.Add(new GeoPointsTransaction
            {
                UserId = user.Id,
                PointsDelta = 100,
                Reason = PointsReason.registration_bonus
            });

            await _db.SaveChangesAsync();       // one round-trip is fine here


            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"{_configuration["FRONTEND_BASE_URL"]}/confirm-email?uid={user.Id}&tok={Uri.EscapeDataString(token)}";

            await _mailer.SendAsync(dto.Email,
                 "Confirm your Geotagger account",
                 $"<p>Hello {dto.Name},</p>" +
                 $"<p>Please confirm your e-mail by clicking the link below:</p>" +
                 $"<p><a href=\"{link}\">Confirm my account</a></p>");


            
            /* ── 3.  e-mail confirmation link
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontend = _config["FRONTEND_BASE_URL"] ?? "http://localhost:5173";
            var url = $"{frontend}/confirm-email?uid={user.Id}&tok={Uri.EscapeDataString(token)}";

            await _mailer.SendAsync(
                    user.Email!,
                    "Confirm your Geotagger account",
                    $@"<h3>Welcome to Geotagger!</h3>
           <p>Please confirm your e-mail by clicking the link below:</p>
           <p><a href=""{url}"">Confirm my account</a></p>");

            */

            return result;
        }



        /// <summary>
        /// Attempts to sign in a user with the provided credentials and returns a JWT on success.
        /// </summary>
        /// <param name="dto">Data transfer object containing login credentials.</param>
        /// <returns>
        /// A JWT string if login succeeds; otherwise <c>null</c>.
        /// </returns>
        public async Task<LoginResult> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || string.IsNullOrWhiteSpace(user.UserName))
                return new LoginResult { Success = false, Error = "Invalid email or password." };

            if (!user.EmailConfirmed)
                return new LoginResult { Success = false, Error = "Please confirm your email before signing in." };

            var signIn = await _signInManager.PasswordSignInAsync(
                user.UserName, dto.Password, isPersistent: false, lockoutOnFailure: false);
            if (!signIn.Succeeded)
                return new LoginResult { Success = false, Error = "Invalid email or password." };

            // 1) generate JWT
            var jwt = await GenerateJwtToken(user);

            // 2) create & persist refresh token
            var newRt = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id
            };
            _db.RefreshTokens.Add(newRt);
            await _db.SaveChangesAsync();

            return new LoginResult
            {
                Success = true,
                Token = jwt,
                RefreshToken = newRt.Token
            };
        }
        public async Task<LoginResult> RefreshTokenAsync(string refreshToken)
        {
            var existing = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (existing == null
                || existing.ExpiresAt < DateTime.UtcNow
                || existing.RevokedAt != null)
            {
                return new LoginResult { Success = false, Error = "Invalid refresh token." };
            }

            // revoke old
            existing.RevokedAt = DateTime.UtcNow;

            // issue new JWT
            var user = await _userManager.FindByIdAsync(existing.UserId);
            var jwt = await GenerateJwtToken(user!);

            // persist rotated refresh-token
            var newRt = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                ReplacedByToken = existing.Token,
                UserId = user!.Id
            };
            _db.RefreshTokens.Add(newRt);
            await _db.SaveChangesAsync();

            return new LoginResult
            {
                Success = true,
                Token = jwt,
                RefreshToken = newRt.Token
            };
        }



        /// <summary>
        /// Generates a JWT token for the specified user based on configuration settings.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <returns>A JWT string.</returns>
        // AuthService.cs  – GenerateJwtToken
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var jwt = _configuration.GetSection("Jwt").Get<JwtSettings>();
            var key = Encoding.UTF8.GetBytes(jwt.Key);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim("id", user.Id)
    };

            // Add external flag if needed (optional)
            var isExternal = await _userManager.IsExternalAsync(user);
            claims.Add(new Claim("external", isExternal ? "1" : "0"));

            // 🔥 Add all user roles as claims
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityTokenHandler().CreateToken(
                new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(jwt.ExpiresInMinutes),
                    Issuer = jwt.Issuer,
                    Audience = jwt.Audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256)
                });

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Initiates a password reset process by generating a reset token and (optionally) emailing it.
        /// </summary>
        /// <param name="dto">Data transfer object containing the user's email.</param>
        /// <returns>A successful <see cref="IdentityResult"/> regardless of email existence.</returns>

        // AuthService.cs  – inside class AuthService
        public async Task<IdentityResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            // 1. Find the user (same response even if not found)
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return IdentityResult.Success;

            // 2. Generate the ASP.NET Identity password reset token
            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 3. Store the token in DB for auditing
            var expiresAt = DateTime.UtcNow.AddHours(1);

            var resetEntry = new GeoPasswordResetToken
            {
                UserId = user.Id,
                Token = identityToken,      // Store the Identity token
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
            _db.Add(resetEntry);
            await _db.SaveChangesAsync();

            // 4. Build the reset-password URL, ensure encode the token for the URL
            var frontend = _config["FRONTEND_BASE_URL"] ?? "http://localhost:5173";
            var resetUrl = $"{frontend}/reset-password?uid={user.Id}&tok={Uri.EscapeDataString(identityToken)}";

            // 5. Send the e-mail
            await _mailer.SendAsync(
                user.Email!,
                "Reset your Geotagger password",
                $@"<p>Hello {user.FirstName},</p>
           <p>You asked to reset your password.</p>
           <p><a href=""{resetUrl}"">Click here to choose a new one</a>.</p>
           <p>This link will expire at {expiresAt:yyyy-MM-dd HH:mm} UTC.</p>
           <p>If you didn’t request this, you can safely ignore this e-mail.</p>");

            // 6. Always succeed to avoid account enumeration
            return IdentityResult.Success;
        }





        /*  public async Task<IdentityResult> ForgotPasswordAsync(ForgotPasswordDto dto)
          {
              var user = await _userManager.FindByEmailAsync(dto.Email);
              if (user == null)
              {
                  // For security, do not reveal user absence
                  return IdentityResult.Success;
              }

              var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
              var frontend = _config["FRONTEND_BASE_URL"] ?? "http://localhost:5173";
              var resetUrl = $"{frontend}/reset-password?uid={user.Id}&tok={Uri.EscapeDataString(resetToken)}";

              await _mailer.SendAsync(
                      user.Email!,
                      "Reset your Geotagger password",
                      $@"<p>You requested a password reset.</p>
             <p><a href=""{resetUrl}"">Click here to choose a new password</a></p>");


              return IdentityResult.Success;
          }*/

        /// <summary>
        /// Resets the user's password using a valid reset token and new password.
        /// </summary>
        /// <param name="dto">Data transfer object containing reset token and new password details.</param>
        /// <returns>
        /// An <see cref="IdentityResult"/> indicating success or failure.
        /// </returns>
        //reset the users password using the provided token
        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            // 0. Validate the reset-token record
            var resetEntry = await _db.GeoPasswordResetTokens
                .FirstOrDefaultAsync(r =>
                    r.UserId == dto.UserId &&
                    r.Token == dto.Token);

            if (resetEntry == null ||
                resetEntry.ExpiresAt < DateTime.UtcNow ||
                resetEntry.UsedAt != null)
            {
                var err = new IdentityError
                {
                    Code = "InvalidToken",
                    Description = "Reset token is invalid, expired, or already used."
                };
                return IdentityResult.Failed(err);
            }

            // 1. Find the user
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                var error = new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "Could not find the user."
                };
                return IdentityResult.Failed(error);
            }

            // 2. Verify password confirmation
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                var error = new IdentityError
                {
                    Code = "PasswordMismatch",
                    Description = "New password and confirmation do not match."
                };
                return IdentityResult.Failed(error);
            }

            // 3. Perform the reset
            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return result;

            // 4. Mark the token as used
            resetEntry.UsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return IdentityResult.Success;
        }


        // Services/AuthService.cs
        public async Task<string?> ExternalLoginAsync(ExternalLoginDto dto)
        {
          
            /* 1. Validate the provider-specific token                    */
            string? email = dto.Provider.ToLowerInvariant() switch
            {
                "google" => await ValidateGoogleAsync(dto.IdToken),
                "facebook" => await ValidateFacebookAsync(dto.IdToken),
                _ => null
            };
            if (email is null) return null;                // bad token

          
            /* 2. Load or create the Identity user                        */
            var user = await _userManager.FindByEmailAsync(email);
            var isNewIdentityUser = false;

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true
                };
                var cr = await _userManager.CreateAsync(user);
                if (!cr.Succeeded) return null;             // should not happen
                isNewIdentityUser = true;
            }

        
            /* 3. Ensure the GeoUser wallet row exists                    */
           
            var geo = await _db.GeoUsers.FindAsync(user.Id);
            if (geo is null)                               // first-time registration
            {
                geo = new GeoUser                           // initial 100 pts
                {
                    UserId = user.Id,
                    GamePoints = 100
                };
                _db.GeoUsers.Add(geo);

                _db.GeoPointsTransactions.Add(new GeoPointsTransaction
                {
                    UserId = user.Id,
                    PointsDelta = 100,
                    Reason = PointsReason.registration_bonus
                });
            }

           
            /* 4. Persist everything */
      
            await _db.SaveChangesAsync();

        
            // 5. Hand out a fresh JWT*/
            return await GenerateJwtToken(user);
        }


        private async Task<string?> ValidateGoogleAsync(string idToken)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                return payload.Email;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> ValidateFacebookAsync(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://graph.facebook.com/me?fields=email&access_token={accessToken}";
            var res = await client.GetAsync(url);
            if (!res.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("email", out var elm) &&
                elm.ValueKind == JsonValueKind.String)
            {
                return elm.GetString();
            }

            return null;
        }
        // Services/AuthService.cs
        public async Task<string> GenerateJwtForUserAsync(ApplicationUser user)
        {
            var jwtKey = _config["JWT__KEY"] ?? _config["Jwt:Key"];
            var jwtIssuer = _config["JWT__ISSUER"] ?? _config["Jwt:Issuer"];
            var jwtAudience = _config["JWT__AUDIENCE"] ?? _config["Jwt:Audience"];
            var expiresStr = _config["JWT__EXPIRESMIN"] ?? _config["Jwt:ExpiresInMinutes"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException("Missing JWT configuration values.");

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim("id", user.Id),
        new Claim("firstName", user.FirstName ?? string.Empty),
        new Claim("lastName", user.LastName ?? string.Empty),
        new Claim("external", "1")
    };

            // 🔥 Add all roles the user has
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.TryParse(expiresStr, out var m) ? m : 60);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            await EnsureGeoUserAsync(user.Id);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        public async Task EnsureGeoUserAsync(string userId)
        {
            var geo = await _db.GeoUsers.FindAsync(userId);

            /* first-time OAuth login → insert wallet row + bonus --------------- */
            if (geo == null)
            {
                geo = new GeoUser                // initial 100 pts
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
            /* early bug left some rows with 0 pts – patch them on the fly ------ */
            else if (geo.GamePoints == 0)
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




    }
}