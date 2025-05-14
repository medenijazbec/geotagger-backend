using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace geotagger_backend.Services
{
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            ApplicationDbContext db,
            IWebHostEnvironment env,
            ILogger<LocationService> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // geotagger_backend/Services/LocationService.cs
        public async Task<LocationDto> UploadLocationAsync(string userId, LocationUploadDto dto, string bucketBaseUrl)
        {
            // validate coords...
            if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
                throw new ArgumentOutOfRangeException("Coordinates out of valid range.");

            // 1) point at wwwroot/images
            var folder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(folder);

            // 2) unique filename: GUID + original extension
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(dto.Image.FileName)}";
            var filePath = Path.Combine(folder, fileName);

            // 3) save to disk
            await using var fs = System.IO.File.Create(filePath);
            await dto.Image.CopyToAsync(fs);

            // 4) build your EF entity
            var loc = new GeoLocation
            {
                UploaderId = userId,
                S3OriginalKey = $"images/{fileName}",    // your “path” inside wwwroot
                Title = dto.Title,
                Description = dto.Description,
                Latitude = Math.Round(dto.Latitude, 6),
                Longitude = Math.Round(dto.Longitude, 6)
            };

            await _db.GeoLocations.AddAsync(loc);
            await _db.SaveChangesAsync();

            // 5) return the DTO, pointing at /images/<guid>
            return new LocationDto
            {
                LocationId = loc.LocationId,
                Title = loc.Title ?? string.Empty,
                Description = loc.Description,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                ImageUrl = $"{bucketBaseUrl}/images/{fileName}"
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

        public Task<int> CountActiveAsync()
            => _db.GeoLocations.CountAsync(l => l.IsActive);

        public async Task<LocationDto> GetRandomActiveAsync(int offset)
        {
            var l = await _db.GeoLocations
                             .Where(lc => lc.IsActive)
                             .OrderBy(lc => lc.LocationId)
                             .Skip(offset)
                             .FirstOrDefaultAsync();

            if (l == null)
                throw new InvalidOperationException("No active locations.");

            return new LocationDto
            {
                LocationId = l.LocationId,
                Title = l.Title ?? string.Empty,
                Description = l.Description,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                ImageUrl = $"/locations/{Path.GetFileName(l.S3OriginalKey)}"
            };
        }
    }
}
