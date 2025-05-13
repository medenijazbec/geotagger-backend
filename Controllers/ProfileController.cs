using System.Security.Claims;
using System.Threading.Tasks;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuctionService _auctionService;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserManager<ApplicationUser> userManager,
          IAuctionService auctionService, INotificationService notificationService, IWebHostEnvironment env, ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _auctionService = auctionService;
            _notificationService = notificationService;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the currently authenticated <see cref="ApplicationUser"/> based on JWT claims.
        /// </summary>
        /// <returns>
        /// The <see cref="ApplicationUser"/> if authenticated; otherwise <c>null</c>.
        /// </returns>
        private Task<ApplicationUser?> CurrentUserAsync()
        {
            var id = User.FindFirstValue("id") ??
              User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is null ? Task.FromResult<ApplicationUser?>(null) :
              _userManager.FindByIdAsync(id);
        }
        /// <summary>
        /// Retrieves the list of notifications for the current user.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with a list of notifications;
        /// <c>401 Unauthorized</c> if the user is not authenticated.
        /// </returns>
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();
            var list = await _notificationService.GetForUserAsync(user.Id);
            return Ok(list);
        }
        /// <summary>
        /// Marks a specific notification as read.
        /// </summary>
        /// <param name="id">The notification identifier.</param>
        /// <returns>
        /// <c>204 No Content</c> on success;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();
            await _notificationService.MarkAsReadAsync(user.Id, id);
            return NoContent();
        }
        /// <summary>
        /// Marks all notifications for the current user as read.
        /// </summary>
        /// <returns>
        /// <c>204 No Content</c> on success;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        /*mark all as read*/
        [HttpPut("notifications/markAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(user.Id);
            return NoContent();
        }
        /// <summary>
        /// Deletes one of the current user's auctions.
        /// </summary>
        /// <param name="id">The auction identifier.</param>
        /// <returns>
        /// <c>204 No Content</c> on success;
        /// <c>400 Bad Request</c> with error message if deletion fails;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>

        [HttpDelete("auction/{id:int}")]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            try
            {
                await _auctionService.DeleteAuctionAsync(user.Id, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                // log the real exception
                _logger.LogError(ex, "Profile.DeleteAuction failed by user {UserId} on auction {AuctionId}", user.Id, id);
                // return only generic
                return BadRequest(new
                {
                    error = "Could not delete auction."
                });
            }
        }

        /// <summary>
        /// Updates an existing auction for the current user with multipart data support,
        /// including optional image upload.
        /// </summary>
        /// <param name="id">The auction identifier.</param>
        /// <param name="form">Form DTO containing updated auction fields and image.</param>
        /// <returns>
        /// <c>200 OK</c> with the updated auction DTO;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        [HttpPut("auction/{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateAuctionMultipart(
          int id, [FromForm] AuctionUpdateFormDto form)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            /* 1.  optional new image */
            var imgUrl = string.Empty;
            if (form.Image is
                {
                    Length: > 0
                })
            {
                var folder = Path.Combine(_env.WebRootPath, "images");
                Directory.CreateDirectory(folder);
                var name = $"{Guid.NewGuid():N}{Path.GetExtension(form.Image.FileName)}";
                await using
                var fs = System.IO.File.Create(
                  Path.Combine(folder, name));
                await form.Image.CopyToAsync(fs);
                imgUrl = $"/images/{name}";
            }

            /* 2.  forward */
            var dto = new AuctionUpdateDto
            {
                Title = form.Title,
                Description = form.Description,
                StartingPrice = form.StartingPrice,
                StartDateTime = form.StartDateTime,
                EndDateTime = form.EndDateTime,
                MainImageUrl = string.IsNullOrEmpty(imgUrl) ?
                form.ExistingImageUrl // keep old
                :
                imgUrl
            };

            var updated = await _auctionService.UpdateAuctionAsync(user.Id, id, dto);
            return Ok(updated);
        }

        /// <summary>
        /// Retrieves basic profile information for the current user.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with user profile data;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        //  GET api/Profile/me
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
        /// <summary>
        /// Updates the current user's profile fields.
        /// </summary>
        /// <param name="dto">DTO containing fields to update.</param>
        /// <returns>
        /// <c>200 OK</c> with updated profile;
        /// <c>401 Unauthorized</c> if not authenticated;
        /// <c>409 Conflict</c> if email is already taken;
        /// <c>400 Bad Request</c> on other validation errors.
        /// </returns>
        //  PUT api/Profile/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            // only touch fields that were sent
            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase) &&
                  await _userManager.FindByEmailAsync(dto.Email) is not null)
                    return Conflict(new
                    {
                        error = "E‑mail already taken."
                    });

                user.Email = dto.Email;
                user.UserName = dto.Email;
            }
            if (dto.ProfilePictureUrl != null) user.ProfilePictureUrl = dto.ProfilePictureUrl;

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
        /// <summary>
        /// Changes the current user's password.
        /// </summary>
        /// <param name="dto">DTO containing current, new, and confirm passwords.</param>
        /// <returns>
        /// <c>200 OK</c> with confirmation message;
        /// <c>400 Bad Request</c> if passwords mismatch or change fails;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        //  PUT api/Profile/update-password
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new
                {
                    error = "Password mismatch."
                });

            var res = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!res.Succeeded) return BadRequest(res.Errors);

            return Ok(new
            {
                message = "Password updated."
            });
        }
        /// <summary>
        /// Retrieves the list of auctions created by the current user.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with a list of auctions;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        //  GET api/Profile/auctions
        [HttpGet("auctions")]
        public async Task<IActionResult> GetMyAuctions()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var auctions = await _auctionService.GetAuctionsByUserAsync(user.Id);
            return Ok(auctions);
        }
        /// <summary>
        /// Creates a new auction under the current user's profile.
        /// </summary>
        /// <param name="dto">DTO containing auction creation details.</param>
        /// <returns>
        /// <c>200 OK</c> with the created auction DTO;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>
        //  POST api/Profile/auction
        [HttpPost("auction")]
        public async Task<IActionResult> CreateAuction([FromBody] AuctionCreateDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var auction = await _auctionService.CreateAuctionAsync(user.Id, dto);
            return Ok(auction);
        }
        /// <summary>
        /// Retrieves auctions on which the current user has placed bids.
        /// </summary>
        /// <returns>
        /// <c>200 OK</c> with a list of bidding auctions;
        /// <c>401 Unauthorized</c> if not authenticated.
        /// </returns>

        /* GET  api/Profile/bidding  – auctions im currently bidding on */
        [HttpGet("bidding")]
        public async Task<IActionResult> GetBiddingAuctions()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var auctions = await _auctionService.GetAuctionsBiddingAsync(user.Id);
            return Ok(auctions);
        }

        /* GET  api/Profile/won  – auctions I have won */
        [HttpGet("won")]
        public async Task<IActionResult> GetWonAuctions()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var auctions = await _auctionService.GetAuctionsWonAsync(user.Id);
            return Ok(auctions);
        }

        //old update of auction using json
        /*//  PUT api/Profile/auction/{id}
        [HttpPut("auction/{id}")]
        public async Task<IActionResult> UpdateAuction(int id, [FromBody] AuctionUpdateDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            try
            {
                var auction = await _auctionService.UpdateAuctionAsync(user.Id, id, dto);
                return Ok(auction);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }*/
    }
}