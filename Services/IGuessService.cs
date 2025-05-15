using geotagger_backend.DTOs;

namespace geotagger_backend.Services
{
    public interface IGuessService
    {
        Task<GuessResultDto> MakeGuessAsync(string userId, GuessDto dto);
        Task<IEnumerable<PersonalBestDto>> GetPersonalBestsAsync(string userId, int page, int pageSize);
        Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int locationId, int page, int pageSize);
        Task<List<UserGuessDto>> GetAllGuessesAsync(string userId, int page, int pageSize); 
    }
}
