using geotagger_backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AdminController(ApplicationDbContext db) => _db = db;

        [HttpGet("actions")]
        public async Task<IActionResult> LastActions() => Ok(await _db.GeoUserActionLogs
            .OrderByDescending(a => a.ActionTimestamp)
            .Take(100)
            .ToListAsync());
    }
}
