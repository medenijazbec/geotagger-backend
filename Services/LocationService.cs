
// ─────────────────────────────────────────────────────────────────────────────
// File: Services/LocationService.cs
// ─────────────────────────────────────────────────────────────────────────────
using System.IO;
using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace geotagger_backend.Services
{
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocationService> _logger;

        public LocationService(ApplicationDbContext db, IWebHostEnvironment env, ILogger<LocationService> logger)
        {
            _db = db; _env = env; _logger = logger;
        }

        public async Task<LocationDto> UploadLocationAsync(string userId, LocationUploadDto dto, string bucketBaseUrl)
        {
            // basic image validation is assumed done by controller (mime, size, magic bytes).
            var folder = Path.Combine(_env.WebRootPath, "locations");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(dto.Image.FileName)}";
            var path = Path.Combine(folder, fileName);
            await using var fs = System.IO.File.Create(path);
            await dto.Image.CopyToAsync(fs);

            var loc = new GeoLocation
            {
                UploaderId = userId,
                S3OriginalKey = $"locations/{fileName}", // simulated local path
                Title = dto.Title,
                Description = dto.Description,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            // Points ledger: +10
            var tx = new GeoPointsTransaction
            {
                UserId = userId,
                PointsDelta = 10,
                Reason = PointsReason.upload_reward,
                Location = loc
            };

            await _db.GeoLocations.AddAsync(loc);
            await _db.GeoPointsTransactions.AddAsync(tx);
            await UpdateWalletAsync(userId, 10);
            await _db.SaveChangesAsync();

            return new LocationDto
            {
                LocationId = loc.LocationId,
                Title = loc.Title ?? string.Empty,
                Description = loc.Description,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                ImageUrl = $"{bucketBaseUrl}/locations/{fileName}"
            };
        }

        public async Task<IEnumerable<LocationDto>> GetActiveLocationsAsync(int page, int pageSize)
        {
            return await _db.GeoLocations
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LocationDto
                {
                    LocationId = l.LocationId,
                    Title = l.Title ?? string.Empty,
                    Description = l.Description,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    ImageUrl = $"/locations/{Path.GetFileName(l.S3OriginalKey)}"
                })
                .ToListAsync();
        }

        private async Task UpdateWalletAsync(string userId, int delta)
        {
            var gu = await _db.GeoUsers.FindAsync(userId);
            if (gu == null)
            {
                gu = new GeoUser { UserId = userId, GamePoints = 10 + delta };
                _db.GeoUsers.Add(gu);
            }
            else gu.GamePoints += delta;
        }
    }
}