using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace geotagger_backend.Services
{
    public class GuessService : IGuessService
    {
        private readonly ApplicationDbContext _db;
        public GuessService(ApplicationDbContext db) => _db = db;

        public async Task<GuessResultDto> MakeGuessAsync(string userId, GuessDto dto)
        {
            var location = await _db.GeoLocations.FindAsync(dto.LocationId);
            if (location == null || !location.IsActive)
                throw new ArgumentException("Location not found or inactive.");

            var attempts = await _db.GeoGuesses.CountAsync(g => g.LocationId == dto.LocationId && g.UserId == userId);
            var attemptNumber = attempts + 1;
            var cost = attemptNumber switch { 1 => 1, 2 => 2, _ => 3 };

            var wallet = await _db.GeoUsers.FindAsync(userId);
            if (wallet == null || wallet.GamePoints < cost)
                throw new InvalidOperationException("Insufficient game points.");

            double error = Haversine((decimal)location.Latitude, (decimal)location.Longitude, dto.Latitude, dto.Longitude);

            var guess = new GeoGuess
            {
                LocationId = dto.LocationId,
                UserId = userId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ErrorMeters = error,
                AttemptNumber = attemptNumber
            };

            var tx = new GeoPointsTransaction
            {
                UserId = userId,
                PointsDelta = -cost,
                Reason = PointsReason.guess_cost,
                Guess = guess
            };

            wallet.GamePoints -= cost;
            wallet.TotalGuessesMade++;

            _db.GeoGuesses.Add(guess);
            _db.GeoPointsTransactions.Add(tx);
            await _db.SaveChangesAsync();

            return new GuessResultDto
            {
                LocationId = dto.LocationId,
                ErrorMeters = Math.Round(error, 2),
                AttemptNumber = attemptNumber,
                RemainingPoints = wallet.GamePoints
            };
        }

        private static double Haversine(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double R = 6371000; // metres
            var φ1 = (double)lat1 * Math.PI / 180;
            var φ2 = (double)lat2 * Math.PI / 180;
            var Δφ = ((double)lat2 - (double)lat1) * Math.PI / 180;
            var Δλ = ((double)lon2 - (double)lon1) * Math.PI / 180;
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) + Math.Cos(φ1) * Math.Cos(φ2) * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}