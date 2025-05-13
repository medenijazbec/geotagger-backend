using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using geotagger_backend.Data;
using geotagger_backend.Models;
using geotagger_backend.Services;
using geotagger_backend.DTOs;
using geotagger_backend.DTOs.Admin;

namespace geotagger_backend.Controllers
{
    /*[ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        /// <summary>
        /// Controller for administrative actions on users and auctions.
        /// Accessible only to users in the "Admin" role.
        /// </summary>

        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _db;

        private readonly ILogger<AdminController> _logger;

        public AdminController(
          UserManager<ApplicationUser> users,
          ApplicationDbContext db
  
          ILogger<AdminController> logger)
        {
            _users = users;
            _db = db;
        
            _logger = logger;
        }

        /// <summary>
        /// Searches and paginates users based on optional search term (email, first name, or last name).
        /// </summary>
        /// <param name="search">Optional search filter.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>Paged list of users matching the search criteria.</returns>

        // GET api/Admin/users?search=&page=&pageSize=
        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers(
          [FromQuery] string? search,
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 20)
        {
            var query = _users.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u =>
                  u.Email.ToLower().Contains(search) ||
                  u.FirstName.ToLower().Contains(search) ||
                  u.LastName.ToLower().Contains(search));
            }

            var total = await query.CountAsync();
            var users = await query
              .OrderBy(u => u.Email)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(u => new AdminUserListDto
              {
                  Id = u.Id,
                  Email = u.Email!,
                  FirstName = u.FirstName!,
                  LastName = u.LastName!,
                  ProfilePictureUrl = u.ProfilePictureUrl!
              })
              .ToListAsync();

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = users
            });
        }

        /// <summary>
        /// Retrieves detailed information for a specific user, including their auctions.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <returns>User details and list of auctions they created.</returns>

        // GET api/Admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserDetail(string id)
        {
            var u = await _users.FindByIdAsync(id);
            if (u == null) return NotFound();

            var dto = new AdminUserDetailDto
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName!,
                LastName = u.LastName!,
                ProfilePictureUrl = u.ProfilePictureUrl!
            };

            var auctions = await _auctionsSvc.GetAuctionsByUserAsync(u.Id);
            dto.Auctions = auctions.ToList();

            return Ok(dto);
        }

        /// <summary>
        /// Updates user profile information. Ensures email uniqueness and logs errors if update fails.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="dto">Updated user data.</param>
        /// <returns>NoContent on success, or appropriate error status.</returns>

        // PUT api/Admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(
          string id,
          [FromBody] AdminUserUpdateDto dto)
        {
            var u = await _users.FindByIdAsync(id);
            if (u == null) return NotFound();

            // email uniqueness check
            if (!string.Equals(u.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _users.FindByEmailAsync(dto.Email) != null)
                    return Conflict(new
                    {
                        error = "Email already taken."
                    });
            }

            u.FirstName = dto.FirstName;
            u.LastName = dto.LastName;
            u.Email = dto.Email;
            u.UserName = dto.Email;
            if (dto.ProfilePictureUrl != null)
                u.ProfilePictureUrl = dto.ProfilePictureUrl;

            var res = await _users.UpdateAsync(u);
            if (!res.Succeeded)
            {
                // log full details server‐side
                _logger.LogError("Admin.UpdateUser failed for {UserId}: {Errors}",
                  u.Id, string.Join("; ", res.Errors.Select(e => e.Description)));
                // return only a generic error
                return BadRequest(new
                {
                    error = "Could not update user."
                });
            }

            return NoContent();
        }

        /// <summary>
        /// Soft-deletes a user by updating their record (implementation detail may vary).
        /// Logs error if update fails.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <returns>NoContent on success, or appropriate error status.</returns>

        // DELETE api/Admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var u = await _users.FindByIdAsync(id);
            if (u == null) return NotFound();

            var res = await _users.UpdateAsync(u);
            if (!res.Succeeded)
            {
                // log full details server‐side
                _logger.LogError("Admin.UpdateUser failed for {UserId}: {Errors}",
                  u.Id, string.Join("; ", res.Errors.Select(e => e.Description)));
                // return only a generic error
                return BadRequest(new
                {
                    error = "Could not update user."
                });
            }

            return NoContent();
        }

        /// <summary>
        /// Retrieves all auctions created by a specific user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <returns>List of auctions belonging to the user.</returns>

        // GET api/Admin/users/{id}/auctions
        [HttpGet("users/{id}/auctions")]
        public async Task<IActionResult> GetUserAuctions(string id)
        {
            // reuse your auction service
            var list = await _auctionsSvc.GetAuctionsByUserAsync(id);
            return Ok(list);
        }

        /// <summary>
        /// Searches and paginates auctions based on optional search term (title or description).
        /// </summary>
        /// <param name="search">Optional search filter.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>Paged list of auctions matching the search criteria.</returns>

        // GET api/Admin/auctions?search=&page=&pageSize=
        [HttpGet("auctions")]
        public async Task<IActionResult> SearchAuctions(
          [FromQuery] string? search,
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 20)
        {
            var q = _db.Auctions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                // simple title/description search; you can join AspNetUsers similarly
                q = q.Where(a =>
                  a.Title.ToLower().Contains(search) ||
                  a.Description.ToLower().Contains(search));
            }

            var total = await q.CountAsync();
            var slice = await q
              .OrderByDescending(a => a.CreatedAt)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(a => a.AuctionId)
              .ToListAsync();

            // map each id via your service (to get DTO)
            var items = new List<AuctionResponseDto>();
            foreach (var id in slice)
            {
                var dto = await _auctionsSvc.GetAuctionAsync(id);
                if (dto != null) items.Add(dto);
            }

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        /// <summary>
        /// Retrieves detailed information for a specific auction by its ID.
        /// </summary>
        /// <param name="id">Auction ID.</param>
        /// <returns>Full auction detail DTO.</returns>

        // GET api/Admin/auctions/{id}
        [HttpGet("auctions/{id:int}")]
        public async Task<IActionResult> GetAuction(int id)
        {
            var dto = await _auctionsSvc.GetAuctionDetailAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Updates an existing auction's fields and returns the updated DTO.
        /// </summary>
        /// <param name="id">Auction ID.</param>
        /// <param name="dto">Updated auction data.</param>
        /// <returns>Updated auction DTO.</returns>

        // PUT api/Admin/auctions/{id}
        [HttpPut("auctions/{id:int}")]
        public async Task<IActionResult> UpdateAuction(
          int id,
          [FromBody] AdminAuctionUpdateDto dto)
        {
            var a = await _db.Auctions.FindAsync(id);
            if (a == null) return NotFound();

            a.Title = dto.Title;
            a.Description = dto.Description;
            a.StartingPrice = dto.StartingPrice;
            a.StartDateTime = dto.StartDateTime;
            a.EndDateTime = dto.EndDateTime;
            a.MainImageUrl = dto.MainImageUrl ?? a.MainImageUrl;
            a.ThumbnailUrl = dto.ThumbnailUrl ?? a.ThumbnailUrl;
            a.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // return the full DTO so front-end can re-render
            var updated = await _auctionsSvc.GetAuctionAsync(id);
            return Ok(updated);
        }

        /// <summary>
        /// Permanently deletes an auction by its ID.
        /// </summary>
        /// <param name="id">Auction ID.</param>
        /// <returns>NoContent on success, or NotFound if auction does not exist.</returns>

        // DELETE api/Admin/auctions/{id}
        [HttpDelete("auctions/{id:int}")]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            var a = await _db.Auctions.FindAsync(id);
            if (a == null) return NotFound();

            _db.Auctions.Remove(a);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
    */
}

    