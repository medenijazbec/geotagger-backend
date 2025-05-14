using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;

namespace geotagger_backend.Services
{
    public interface ILocationService
    {
        Task<LocationDto> UploadLocationAsync(string userId, LocationUploadDto dto, string bucketBaseUrl);
        Task<IEnumerable<LocationDto>> GetActiveLocationsAsync(int page, int pageSize);
        Task<int> CountActiveAsync();
        Task<LocationDto> GetRandomActiveAsync(int offset);
        Task<IEnumerable<LocationDto>> GetUserLocationsAsync(string userId, int page, int pageSize);
        Task<LocationDto> UpdateLocationAsync(int locationId, string userId, LocationUploadDto dto, string baseUrl);
        Task<LocationDto?> GetByIdAsync(int id);
    }
}

