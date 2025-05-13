using geotagger_backend.DTOs;

namespace geotagger_backend.Services
{
    public interface IGuessService
    {
        Task<GuessResultDto> MakeGuessAsync(string userId, GuessDto dto);
    }
}
