using geotagger_backend.DTOs;
using geotagger_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace geotagger_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GuessController : ControllerBase
    {
        private readonly IGuessService _svc;
        public GuessController(IGuessService svc) => _svc = svc;

        [HttpPost]
        public async Task<IActionResult> MakeGuess([FromBody] GuessDto dto)
        {
            var userId = User.FindFirst("id")?.Value;
            if (userId == null) return Unauthorized();

            try
            {
                var res = await _svc.MakeGuessAsync(userId, dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}