// File: Controllers/ProfileController.cs
using System.Security.Claims;
using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;
        private readonly ILogger<ProfileController> _logger;
        private readonly ILocationService _svc;
        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            INotificationService notifications,
            ILogger<ProfileController> logger,
            ILocationService svc)
        {
            _userManager = userManager;
            _db = db;
            _notifications = notifications;
            _logger = logger;
            _svc = svc;
        }

        /* ------------------------------------------------------------------ */
        /* utility                                                             */
        /* ------------------------------------------------------------------ */

        private Task<ApplicationUser?> CurrentUserAsync()
        {
            var id = User.FindFirstValue("id") ??
                     User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is null
                ? Task.FromResult<ApplicationUser?>(null)
                : _userManager.FindByIdAsync(id);
        }

        /* ------------------------------------------------------------------ */
        /* 1. NOTIFICATIONS                                                   */
        /* ------------------------------------------------------------------ */

        /// GET api/Profile/notifications
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var list = await _notifications.GetForUserAsync(user.Id);
            return Ok(list);
        }

        /// PUT api/Profile/notifications/{id}/read
        [HttpPut("notifications/{id:int}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            await _notifications.MarkAsReadAsync(user.Id, id);
            return NoContent();
        }

        /// PUT api/Profile/notifications/markAllRead
        [HttpPut("notifications/markAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            await _notifications.MarkAllAsReadAsync(user.Id);
            return NoContent();
        }

        /* ------------------------------------------------------------------ */
        /* 2. BASIC PROFILE                                                   */
        /* ------------------------------------------------------------------ */

        /// GET api/Profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        /// PUT api/Profile/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                    return Conflict(new { error = "E-mail already taken." });

                user.Email = dto.Email;
                user.UserName = dto.Email;
            }

            if (dto.ProfilePictureUrl != null)
                user.ProfilePictureUrl = dto.ProfilePictureUrl;

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
                return BadRequest(res.Errors.FirstOrDefault()?.Description ?? "Update failed.");

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        /// PUT api/Profile/update-password
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { error = "Password mismatch." });

            var res = await _userManager.ChangePasswordAsync(
                user, dto.CurrentPassword, dto.NewPassword);

            if (!res.Succeeded) return BadRequest(res.Errors);
            return Ok(new { message = "Password updated." });
        }

        /* ------------------------------------------------------------------ */
        /* 3. WALLET / GAME STATS                                             */
        /* ------------------------------------------------------------------ */

        /// GET api/Profile/wallet
        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var geo = await _db.GeoUsers.FindAsync(user.Id);

            return Ok(new
            {
                points = geo?.GamePoints ?? 0,
                totalLocations = geo?.TotalLocationsUploaded ?? 0,
                totalGuesses = geo?.TotalGuessesMade ?? 0
            });
        }

        /// <summary>
        /// GET api/Profile/locations
        /// Returns the current user's uploaded locations (paginated).
        /// </summary>
        // Controllers/ProfileController.cs

        [HttpGet("locations")]
        public async Task<IActionResult> GetMyLocations([FromQuery] int page = 1, [FromQuery] int size = 4)
        {
            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();

            var dtos = await _svc.GetUserLocationsAsync(user.Id, page, size);
            return Ok(dtos);
        }

    }
}
