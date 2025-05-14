using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _svc;
        private readonly IConfiguration _cfg;
        private readonly ApplicationDbContext _db;
        public LocationsController(ILocationService svc, IConfiguration cfg, ApplicationDbContext db) 
        { _svc = svc; 
          _cfg = cfg;
            _db = db;
        }

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

        [HttpGet]
        public async Task<IActionResult> Browse([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var list = await _svc.GetActiveLocationsAsync(page, size);
            return Ok(list);
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomLocation()
        {
            var total = await _svc.CountActiveAsync();
            if (total == 0) return NotFound();

            var offset = Random.Shared.Next(total);
            var dto = await _svc.GetRandomActiveAsync(offset);
            return Ok(dto);
        }
        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateLocation([FromRoute] int id, [FromForm] LocationUploadDto dto)
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
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLocation([FromRoute] int id)
        {
            // 1. make sure the user is authenticated
            var userId = User.FindFirst("id")?.Value;
            if (userId == null)
                return Unauthorized();

            // 2. fetch the location
            var loc = await _db.GeoLocations.FindAsync(id);
            if (loc == null)
                return NotFound();

            // 3. ensure they own it
            if (loc.UploaderId != userId)
                return Forbid();

            // 4a. if you want to soft‐delete:
            loc.IsActive = false;

            // 4b. or to hard‐delete:
            // _db.GeoLocations.Remove(loc);

            await _db.SaveChangesAsync();

            // 5. 204 No Content on success
            return NoContent();
        }


    }
}