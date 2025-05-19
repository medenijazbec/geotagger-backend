using System.ComponentModel.DataAnnotations;     // ← for data-annotations if you need them
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using geotagger_backend.Data;
using geotagger_backend.Models;
using geotagger_backend.DTOs;

namespace geotagger_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    /*──────────────────────────────  DTOs  ──────────────────────────────*/

    public record PagedResult<T>(int Total, IEnumerable<T> Items);

    /*──────────────────────────────  USERS  ──────────────────────────────*/

    // GET: api/Admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(u =>
                u.Email!.Contains(term) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term));
        }

        var total = await q.CountAsync();

        var items = await q.OrderBy(u => u.Email)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .Select(u => new UserSummaryDto(
                               u.Id,
                               u.Email!,
                               u.FirstName,
                               u.LastName,
                               u.ProfilePictureUrl))
                           .ToListAsync();

        return Ok(new PagedResult<UserSummaryDto>(total, items));
    }

    // GET: api/Admin/users/{id}
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var locationsForUser = _db.GeoLocations
                                  .Where(l => l.UploaderId == id)
                                  .OrderByDescending(l => l.CreatedAt)
                                  .Select(l => new LocationSummaryDto(
                                      l.LocationId, l.Title, l.IsActive));

        var user = await _db.Users
                            .AsNoTracking()
                            .Where(u => u.Id == id)
                            .Select(u => new UserDetailDto(
                                u.Id,
                                u.Email!,
                                u.FirstName,
                                u.LastName,
                                u.ProfilePictureUrl,
                                locationsForUser))
                            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

    // PUT: api/Admin/users/{id}
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UserSummaryDto dto)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.Email = dto.Email;
        user.UserName = dto.Email;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.ProfilePictureUrl = dto.ProfilePictureUrl;

        var res = await _users.UpdateAsync(user);
        return res.Succeeded ? Ok() : BadRequest(res.Errors);
    }

    // DELETE: api/Admin/users/{id}
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var res = await _users.DeleteAsync(user);
        return res.Succeeded ? NoContent() : BadRequest(res.Errors);
    }

    /*──────────────────────────────  LOCATIONS  ──────────────────────────────*/

    // GET: api/Admin/locations
    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations(
     [FromQuery] string? search = null,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.GeoLocations
                   .Include(l => l.Uploader)
                       .ThenInclude(u => u.Identity)
                   .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(l => l.Title.Contains(term) || l.Description.Contains(term));
        }

        var total = await q.CountAsync();

        var items = await q.OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LocationListDto(
                l.LocationId,
                l.Title,
                l.Uploader.Identity != null
                    ? (l.Uploader.Identity.FirstName + " " + l.Uploader.Identity.LastName)
                    : "(unknown)",
                l.CreatedAt,
                l.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<LocationListDto>(total, items));
    }


    // GET: api/Admin/locations/{id}
    [HttpGet("locations/{id:int}")]
    public async Task<IActionResult> GetLocation(int id)
    {
        var loc = await _db.GeoLocations
            .Include(l => l.Uploader)
                .ThenInclude(u => u.Identity)
            .AsNoTracking()
            .Where(l => l.LocationId == id)
            .Select(l => new LocationDetailDto(
                l.LocationId,
                l.Title,
                l.Description,
                l.Latitude,
                l.Longitude,
                l.IsActive,
                l.CreatedAt,
                l.UploaderId,
                l.Uploader.Identity != null
                    ? (l.Uploader.Identity.FirstName + " " + l.Uploader.Identity.LastName)
                    : "(unknown)",
                l.S3OriginalKey,
                l.S3ThumbnailKey))
            .FirstOrDefaultAsync();

        return loc is null ? NotFound() : Ok(loc);
    }


    // PUT: api/Admin/locations/{id}
    [HttpPut("locations/{id:int}")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationDto dto)
    {
        var loc = await _db.GeoLocations.FindAsync(id);
        if (loc is null) return NotFound();

        loc.Title = dto.Title;
        loc.Description = dto.Description;
        loc.Latitude = dto.Latitude;
        loc.Longitude = dto.Longitude;
        loc.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return Ok();
    }

    // DELETE: api/Admin/locations/{id}
    [HttpDelete("locations/{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var loc = await _db.GeoLocations.FindAsync(id);
        if (loc is null) return NotFound();

        _db.GeoLocations.Remove(loc);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /*──────────────────────────────  ACTIVITY LOGS  ──────────────────────────────*/


    // GET: api/Admin/activity-log
    [HttpGet("activity-log")]
    public async Task<IActionResult> GetActivityLog(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 100)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var q = from log in _db.GeoUserActionLogs
                join user in _db.Users on log.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                orderby log.ActionTimestamp descending
                select new ActivityLogDto(
                    log.ActionId,
                    log.UserId,
                    user != null ? user.Email : null,
                    user != null ? user.FirstName : null,
                    user != null ? user.LastName : null,
                    log.ActionType,
                    log.ComponentType,
                    log.NewValue,
                    log.Url,
                    log.ActionTimestamp
                );

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<ActivityLogDto>(total, items));
    }





    // DELETE: api/Admin/activity-log/{id}
    [HttpDelete("activity-log/{id:long}")]
    public async Task<IActionResult> DeleteLog(long id)
    {
        var log = await _db.GeoUserActionLogs.FindAsync(id);
        if (log is null) return NotFound();

        _db.GeoUserActionLogs.Remove(log);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
