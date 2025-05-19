// using-directives unchanged …
using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace geotagger_backend.Controllers;

[ApiController]
[Route("api/log")]
public class LogController : ControllerBase
{
    private static readonly HashSet<string> AllowedActionTypes = new(new[] {
        "click", "scroll", "added_value", "changed_value", "removed_value"
    });

    private readonly ApplicationDbContext _db;
    private readonly ILogger<LogController> _logger;
    private readonly UserManager<ApplicationUser> _users;    

    public LogController(
        ApplicationDbContext db,
        ILogger<LogController> logger,
        UserManager<ApplicationUser> users)                 
    {
        _db = db;
        _logger = logger;
        _users = users;                                    
    }

    
    [AllowAnonymous]
    [HttpPost("log-action")]
    public async Task<IActionResult> LogAction([FromBody] GeoUserActionLogDto dto)
    {
        //  pick GUID from JWT if present
        string? uid = User.FindFirstValue("id")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        // guests may supply their own temporary id
        if (string.IsNullOrWhiteSpace(uid))
            uid = dto.UserId;

        //  if what we got looks like an e-mail, translate it → Guid
        if (!string.IsNullOrWhiteSpace(uid) && uid.Contains('@'))
        {
            uid = await _users.Users
                              .Where(u => u.Email == uid)
                              .Select(u => u.Id)
                              .FirstOrDefaultAsync();
        }

        //  final guard
        if (string.IsNullOrWhiteSpace(uid) || !Guid.TryParse(uid, out _))
            return BadRequest("Unable to resolve a valid user GUID.");

        var log = new GeoUserActionLog
        {
            UserId = uid,
            ActionType = (dto.ActionType ?? "click").Trim().ToLowerInvariant(),
            ComponentType = dto.ComponentType,
            NewValue = dto.NewValue,
            Url = dto.Url,
            ActionTimestamp = DateTime.UtcNow
        };

        _db.GeoUserActionLogs.Add(log);
        await _db.SaveChangesAsync();
        return Ok();
    }

    /*BATCH / CLIENT-ACTION */

    [AllowAnonymous]
    [HttpPost("client-action")]
    public async Task<IActionResult> LogClientAction([FromBody] JsonElement json)
    {
        // Parse array OR single object
        var incoming = json.ValueKind switch
        {
            JsonValueKind.Array => JsonSerializer.Deserialize<List<ClientActionLogDto>>(json.GetRawText()),
            JsonValueKind.Object => new List<ClientActionLogDto?> { JsonSerializer.Deserialize<ClientActionLogDto>(json.GetRawText()) }
                                    .Where(x => x is not null).Cast<ClientActionLogDto>().ToList(),
            _ => null
        };

        if (incoming is null or { Count: 0 })
            return BadRequest("Invalid data.");

        string? tokenUid = User.FindFirstValue("id")
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        /* helper to coerce any string into a Guid (or "anonymous") */
        async Task<string> ResolveUidAsync(string? candidate)
        {
            var uid = tokenUid ?? candidate;

            if (string.IsNullOrWhiteSpace(uid))
                return "anonymous";

            if (uid.Contains('@'))                           // e-mail -< translate
            {
                uid = await _users.Users
                                  .Where(u => u.Email == uid)
                                  .Select(u => u.Id)
                                  .FirstOrDefaultAsync();
            }

            return Guid.TryParse(uid, out _) ? uid! : "anonymous";
        }

        var dbLogs = new List<GeoUserActionLog>();

        foreach (var dto in incoming)
        {
            var uid = await ResolveUidAsync(dto.UserId);

            var act = (dto.ActionType ?? "click").Trim().ToLowerInvariant();
            if (!AllowedActionTypes.Contains(act)) act = "click";

            dbLogs.Add(new GeoUserActionLog
            {
                UserId = uid,
                ActionType = act,
                ComponentType = dto.ComponentType,
                NewValue = dto.NewValue,
                Url = dto.Url,
                ActionTimestamp = dto.ActionTimestamp ?? DateTime.UtcNow
            });
        }

        await _db.GeoUserActionLogs.AddRangeAsync(dbLogs);
        await _db.SaveChangesAsync();
        return Ok(new { saved = dbLogs.Count });
    }
}
