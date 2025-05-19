using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Route("api/log")]
    public class LogController : ControllerBase
    {
        private static readonly HashSet<string> AllowedActionTypes = new(new[] {
            "click", "scroll", "added_value", "changed_value", "removed_value"
        });

        private readonly ApplicationDbContext _db;
        private readonly ILogger<LogController> _logger;

        public LogController(ApplicationDbContext db, ILogger<LogController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("log-action")]
        public async Task<IActionResult> LogAction([FromBody] GeoUserActionLogDto dto)
        {
            // Always prefer the User GUID from JWT if authenticated
            string? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // this is the GUID!
            }
            // Only fallback to dto.UserId if not authenticated (for guests)
            if (string.IsNullOrEmpty(userId)) userId = dto.UserId;

            // If still no userId (should almost never happen), reject
            if (string.IsNullOrEmpty(userId))
                return BadRequest("No user id");

            var log = new GeoUserActionLog
            {
                UserId = userId,
                ActionType = dto.ActionType,
                ComponentType = dto.ComponentType,
                NewValue = dto.NewValue,
                Url = dto.Url,
                ActionTimestamp = DateTime.UtcNow
            };

            _db.GeoUserActionLogs.Add(log);
            await _db.SaveChangesAsync();

            return Ok();
        }




        // Accept both single object and array (RTK Query, Axios, etc. might send either)
        [HttpPost("client-action")]
        [AllowAnonymous]
        public async Task<IActionResult> LogClientAction([FromBody] JsonElement logsElement)
        {
            List<ClientActionLogDto> logs;
            try
            {
                if (logsElement.ValueKind == JsonValueKind.Array)
                {
                    logs = JsonSerializer.Deserialize<List<ClientActionLogDto>>(logsElement.GetRawText());
                }
                else if (logsElement.ValueKind == JsonValueKind.Object)
                {
                    var single = JsonSerializer.Deserialize<ClientActionLogDto>(logsElement.GetRawText());
                    logs = single != null ? new List<ClientActionLogDto> { single } : new List<ClientActionLogDto>();
                }
                else
                {
                    return BadRequest(new { code = "invalid_format", message = "Invalid request format." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse client action log(s).");
                return BadRequest(new { code = "invalid_format", message = "Invalid request body." });
            }

            if (logs == null || logs.Count == 0)
                return BadRequest(new { code = "no_log_entries", message = "No log entries received." });

            var userIdFromToken = User.FindFirst("id")?.Value;

            var dbLogs = logs.Select(l =>
            {
                var actionType = (l.ActionType ?? "click").Trim().ToLowerInvariant();
                if (!AllowedActionTypes.Contains(actionType))
                    actionType = "click"; // fallback for unknown type

                return new GeoUserActionLog
                {
                    UserId = l.UserId ?? userIdFromToken ?? "anonymous",
                    ActionType = actionType,
                    ComponentType = l.ComponentType,
                    NewValue = l.NewValue,
                    Url = l.Url,
                    ActionTimestamp = l.ActionTimestamp ?? DateTime.UtcNow
                };
            }).ToList();

            await _db.GeoUserActionLogs.AddRangeAsync(dbLogs);
            await _db.SaveChangesAsync();

            return Ok(new { code = "ok", message = $"{dbLogs.Count} log(s) saved." });
        }
    }
}
