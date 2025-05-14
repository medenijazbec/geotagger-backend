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
        public LocationsController(ILocationService svc, IConfiguration cfg) { _svc = svc; _cfg = cfg; }

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
    }
}