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
        public async Task<LocationDto> UploadLocationAsync(
            string userId,
            LocationUploadDto dto,
            string bucketBaseUrl)
        {
            /* ── 0.  BASIC VALIDATION ───────────────────────────────────────── */
            if (dto.Latitude is < -90 or > 90 ||
                dto.Longitude is < -180 or > 180)
                throw new ArgumentOutOfRangeException("Coordinates out of valid range.");

            /* ── 1.  SAVE IMAGE TO wwwroot/images/ ──────────────────────────── */
            var imagesFolder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(imagesFolder);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(dto.Image.FileName)}";
            var filePath = Path.Combine(imagesFolder, fileName);

            await using (var fs = System.IO.File.Create(filePath))
                await dto.Image.CopyToAsync(fs);

            /* ── 2.  INSERT GeoLocation ─────────────────────────────────────── */
            var loc = new GeoLocation
            {
                UploaderId = userId,
                S3OriginalKey = $"images/{fileName}",   // relative path under wwwroot
                Title = dto.Title,
                Description = dto.Description,
                Latitude = Math.Round(dto.Latitude, 6),
                Longitude = Math.Round(dto.Longitude, 6)
            };

            await _db.GeoLocations.AddAsync(loc);
            await _db.SaveChangesAsync();             // loc.LocationId is now available

            /* ── 3.  REWARD UPLOADER (+10 pts) & UPDATE STATS ───────────────── */
            var geo = await _db.GeoUsers.FindAsync(userId);
            if (geo != null)
            {
                geo.GamePoints += 10;
                geo.TotalLocationsUploaded += 1;

                _db.GeoPointsTransactions.Add(new GeoPointsTransaction
                {
                    UserId = userId,
                    LocationId = loc.LocationId,
                    PointsDelta = 10,
                    Reason = PointsReason.upload_reward
                });

                await _db.SaveChangesAsync();
            }

            /* ── 4.  RETURN DTO ─────────────────────────────────────────────── */
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


        public async Task<IEnumerable<LocationDto>> GetUserLocationsAsync(
     string userId, int page, int pageSize)
        {
            return await _db.GeoLocations
                .Where(l => l.UploaderId == userId && l.IsActive)
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
                   
                    ImageUrl = $"/images/{Path.GetFileName(l.S3OriginalKey)}"
                })
                .ToListAsync();
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
                    // S3OriginalKey already starts with "images/…"
                    // Prefix with a slash so React can request /images/<file>
                    ImageUrl = $"/{l.S3OriginalKey}"
                })
                .ToListAsync();
        }
        public async Task<LocationDto?> GetByIdAsync(int id)
        {
            var l = await _db.GeoLocations
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.LocationId == id && x.IsActive);

            return l == null ? null : new LocationDto
            {
                LocationId = l.LocationId,
                Title = l.Title ?? string.Empty,
                Description = l.Description,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                ImageUrl = $"/{l.S3OriginalKey}"
            };
        }
        public Task<int> CountActiveAsync()
            => _db.GeoLocations.CountAsync(l => l.IsActive);

        public async Task<LocationDto> GetRandomActiveAsync(int offset)
        {
            var l = await _db.GeoLocations
                             .Where(lc => lc.IsActive)
                             .OrderBy(lc => lc.LocationId)   // deterministic order
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

                //  point to static /images/… instead of the non-existent /locations/
                ImageUrl = $"/{l.S3OriginalKey}"           //  /images/abcd1234.jpg”
            };
        }
        public async Task<LocationDto> UpdateLocationAsync(int locationId, string userId, LocationUploadDto dto, string baseUrl)
        {
            // 1) fetch existing
            var loc = await _db.GeoLocations.FindAsync(locationId);
            if (loc == null || loc.UploaderId != userId)
                throw new ArgumentException("Location not found or not owned by user.");

            // 2) delete old file
            var oldKey = loc.S3OriginalKey;                    //  "images/{guid}.jpg"
            var oldPath = Path.Combine(_env.WebRootPath, oldKey);
            if (File.Exists(oldPath)) File.Delete(oldPath);

            // 3) save new file
            var folder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(folder);
            var fname = $"{Guid.NewGuid():N}{Path.GetExtension(dto.Image.FileName)}";
            var filepath = Path.Combine(folder, fname);
            await using var fs = File.Create(filepath);
            await dto.Image.CopyToAsync(fs);

            // 4) update entity
            loc.S3OriginalKey = $"images/{fname}";
            loc.Title = dto.Title;
            loc.Description = dto.Description;
            loc.Latitude = Math.Round(dto.Latitude, 6);
            loc.Longitude = Math.Round(dto.Longitude, 6);
            await _db.SaveChangesAsync();

            // 5) return updated DTO
            return new LocationDto
            {
                LocationId = loc.LocationId,
                Title = loc.Title!,
                Description = loc.Description,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                ImageUrl = $"{baseUrl}/images/{fname}"
            };
        }
    }
}
