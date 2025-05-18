using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _svc;
        private readonly IConfiguration _cfg;
        private readonly ApplicationDbContext _db;

        public LocationsController(
            ILocationService svc,
            IConfiguration cfg,
            ApplicationDbContext db)
        {
            _svc = svc;
            _cfg = cfg;
            _db = db;
        }

        /* ──────────────────────────────────────────────────────────────── */
        /* 0.  UPLOAD                                                      */
        /* ──────────────────────────────────────────────────────────────── */

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] LocationUploadDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            var loc = await _svc.UploadLocationAsync(userId, dto, baseUrl);

            return Ok(loc);
        }

        /* ──────────────────────────────────────────────────────────────── */
        /* 1.  PAGED BROWSE – accepts both size= and pageSize=             */
        /* ──────────────────────────────────────────────────────────────── */

        [HttpGet]
        public async Task<IActionResult> Browse(
            [FromQuery] int page = 1,
            [FromQuery] int? size = null,                                         // legacy param
            [FromQuery(Name = "pageSize")] int? pageSize = null)                       // new param (React)
        {
            var take = pageSize ?? size ?? 20;
            if (take <= 0) take = 20;

            var list = await _svc.GetActiveLocationsAsync(page, take);
            return Ok(list);
        }

        /* ──────────────────────────────────────────────────────────────── */
        /* 2.  SINGLE LOCATION  – used on the guess page                   */
        /* ──────────────────────────────────────────────────────────────── */

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _svc.GetByIdAsync(id);
            return dto == null ? NotFound() : Ok(dto);
        }

        /* ──────────────────────────────────────────────────────────────── */
        /* 3.  RANDOM LOCATION                                             */
        /* ──────────────────────────────────────────────────────────────── */

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomLocation()
        {
            var total = await _svc.CountActiveAsync();
            if (total == 0) return NotFound();

            var offset = Random.Shared.Next(total);
            var dto = await _svc.GetRandomActiveAsync(offset);

            return Ok(dto);
        }

        /* ──────────────────────────────────────────────────────────────── */
        /* 4.  UPDATE (multipart/form-data)                                */
        /* ──────────────────────────────────────────────────────────────── */

        /*
        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateLocation(
            int id,
            [FromForm] LocationUploadDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (userId == null) return Unauthorized();

            var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";

            try
            {
                var updated = await _svc.UpdateLocationAsync(id, userId, dto, baseUrl);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }*/


        //usses midleware to format excetion etc
        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateLocation(int id,[FromForm] LocationUploadDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (userId == null) return Unauthorized();

            var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";

            var updated = await _svc.UpdateLocationAsync(id, userId, dto, baseUrl);
            return Ok(updated);
        }


        /* ──────────────────────────────────────────────────────────────── */
        /* 5.  DELETE (soft-delete by default)                             */
        /* ──────────────────────────────────────────────────────────────── */

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            /* 1. ensure user is authenticated */
            var userId = User.FindFirst("id")?.Value;
            if (userId == null)
                return Unauthorized();

            /* 2. fetch the location */
            var loc = await _db.GeoLocations.FindAsync(id);
            if (loc == null)
                return NotFound();

            /* 3. verify ownership */
            if (loc.UploaderId != userId)
                return Forbid();

            /* 4. soft-delete (set inactive) */
            loc.IsActive = false;

            //  ─ OR ─   hard-delete:
            // _db.GeoLocations.Remove(loc);

            await _db.SaveChangesAsync();

            /* 5. HTTP 204 No Content */
            return NoContent();
        }
    }
}
