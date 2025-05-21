using geotagger_backend.Data;
using geotagger_backend.DTOs;
using geotagger_backend.Models;
using Microsoft.EntityFrameworkCore;
using ZstdSharp.Unsafe;

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

            // 1) figure out attempt number + cost
            var attempts = await _db.GeoGuesses.CountAsync(g =>
                g.LocationId == dto.LocationId && g.UserId == userId);
            var attemptNumber = attempts + 1;
            var cost = attemptNumber switch { 1 => 1, 2 => 2, _ => 3 };

            // 2) check wallet
            var wallet = await _db.GeoUsers.FindAsync(userId);
            if (wallet == null || wallet.GamePoints < cost)
                throw new InvalidOperationException("Insufficient game points.");

            // 3) compute error
            double error = Haversine(
                (decimal)location.Latitude,
                (decimal)location.Longitude,
                dto.Latitude,
                dto.Longitude);

            // 4) create the guess entity
            var guess = new GeoGuess
            {
                LocationId = dto.LocationId,
                UserId = userId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ErrorMeters = error,
                AttemptNumber = attemptNumber
            };

            // 5) create the ledger transaction (deduct cost)
            var tx = new GeoPointsTransaction
            {
                UserId = userId,
                PointsDelta = -cost,
                Reason = PointsReason.guess_cost,
                Guess = guess
            };

            // 6) update wallet (deduct)
            wallet.GamePoints -= cost;
            wallet.TotalGuessesMade++;

            // ** NEW: 7) Calculate and award accuracy points **
            int baseAward = 1000;
            double factor = 2.0; // Lose 2 points per meter error
            int award = Math.Max(0, (int)Math.Round(baseAward - error * factor));

            GeoPointsTransaction? txAward = null;
            if (award > 0)
            {
                txAward = new GeoPointsTransaction
                {
                    UserId = userId,
                    PointsDelta = award,
                    Reason = PointsReason.guess_accuracy_reward,
                    Guess = guess
                };
                wallet.GamePoints += award;
                _db.GeoPointsTransactions.Add(txAward);
            }
            // ** END NEW **

            // 8) stage and save (as before)
            _db.GeoGuesses.Add(guess);
            _db.GeoPointsTransactions.Add(tx);

            await _db.SaveChangesAsync();

            return new GuessResultDto
            {
                LocationId = dto.LocationId,
                ErrorMeters = Math.Round(error, 2),
                AttemptNumber = attemptNumber,
                RemainingPoints = wallet.GamePoints,
                AwardedPoints = award   // <-- add this property to your DTO if you want to display it
            };
        }



        /*anti-join/NOT EXISTS pattern makes sure i only take the best guess for each location for the user (lowest error earliest if tie).
        */
        public async Task<IEnumerable<PersonalBestDto>> GetPersonalBestsAsync(string userId, int page, int pageSize)
        {
            // All guesses for this user
            var guesses = _db.GeoGuesses.Where(g => g.UserId == userId);

            // For each guess, include only if it is the best (lowest error, earliest tie-break) for that location
            var bestGuesses =
                from guess in guesses
                where !guesses.Any(other =>
                    other.LocationId == guess.LocationId &&
                    //someone else's guess is better for the same location
                    (other.ErrorMeters < guess.ErrorMeters ||
                     (other.ErrorMeters == guess.ErrorMeters && other.CreatedAt < guess.CreatedAt))
                )
                join loc in _db.GeoLocations on guess.LocationId equals loc.LocationId
                orderby guess.ErrorMeters, guess.CreatedAt
                select new PersonalBestDto
                {
                    LocationId = guess.LocationId,
                    ErrorMeters = Math.Round(guess.ErrorMeters, 1),
                    ImageUrl = "/images/" + System.IO.Path.GetFileName(loc.S3OriginalKey)
                };

            return await bestGuesses
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }




        public async Task<List<UserGuessDto>> GetAllGuessesAsync(string userId, int page, int pageSize)
        {
            var guesses = await _db.GeoGuesses
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Join(_db.GeoLocations,
                      guess => guess.LocationId,
                      loc => loc.LocationId,
                      (guess, loc) => new UserGuessDto
                      {
                          LocationId = guess.LocationId,
                          ErrorMeters = Math.Round(guess.ErrorMeters, 1),
                          ImageUrl = "/images/" + System.IO.Path.GetFileName(loc.S3OriginalKey)
                      })
                .ToListAsync();

            return guesses;
        }






        /*public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int locationId, int page, int pageSize)
        {
            // take each user's BEST guess for that location,
            // order globally by error ascending
            return await _db.GeoGuesses
                .Where(g => g.LocationId == locationId)
                .GroupBy(g => g.UserId)
                .Select(grp => grp.OrderBy(x => x.ErrorMeters).First())
                .OrderBy(x => x.ErrorMeters)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Join(_db.Users, g => g.UserId, u => u.Id,
                    (g, u) => new LeaderboardEntryDto
                    {
                        UserId = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        ProfilePictureUrl = u.ProfilePictureUrl,
                        ErrorMeters = Math.Round(g.ErrorMeters, 1),
                        GuessedAt = g.CreatedAt
                    })
                .ToListAsync();
        }
        USERS BEST ATTEMPT*/

        /* public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int locationId, int page, int pageSize)
         {
             return await _db.GeoGuesses
                 .Where(g => g.LocationId == locationId)
                 .OrderBy(g => g.ErrorMeters)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .Join(
                     _db.Users,
                     guess => guess.UserId,
                     user => user.Id,
                     (guess, user) => new LeaderboardEntryDto
                     {
                         UserId = user.Id,
                         FirstName = user.FirstName,
                         LastName = user.LastName,
                         ProfilePictureUrl = user.ProfilePictureUrl,
                         ErrorMeters = Math.Round(guess.ErrorMeters, 1),
                         GuessedAt = guess.CreatedAt
                     }
                 )
                 .ToListAsync();
         }*/

        //watafak... linq je...dkpajsdhjaw98hd7ada78ihgd7a
        /*raw sql
        SELECT
  u.UserName,
  u.ProfilePictureUrl,
  g.UserId,
  g.LocationId,
  g.ErrorMeters      AS BestErrorMeters,
  g.CreatedAt        AS BestGuessAt
FROM (
  SELECT
    *,
    ROW_NUMBER() OVER (
      PARTITION BY UserId
      ORDER BY ErrorMeters ASC, CreatedAt ASC
    ) AS rn
  FROM GeoGuesses
  WHERE LocationId = 6
) g
JOIN AspNetUsers u
  ON g.UserId = u.Id
WHERE g.rn = 1
ORDER BY g.ErrorMeters ASC, g.CreatedAt ASC
LIMIT 0, 1000;

        
        */
        public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int locationId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var guesses = _db.GeoGuesses
                .Where(g => g.LocationId == locationId);

            var leaderboard = from guess in guesses
                              join user in _db.Users on guess.UserId equals user.Id
                              // Only include this guess if there does NOT exist another guess by the same user that's "better"
                              where !guesses.Any(other =>
                                  other.UserId == guess.UserId &&
                                  (other.ErrorMeters < guess.ErrorMeters ||
                                   (other.ErrorMeters == guess.ErrorMeters && other.CreatedAt < guess.CreatedAt))
                              )
                              select new LeaderboardEntryDto
                              {
                                  UserId = user.Id,
                                  FirstName = user.FirstName,
                                  LastName = user.LastName,
                                  ProfilePictureUrl = user.ProfilePictureUrl,
                                  ErrorMeters = Math.Round(guess.ErrorMeters, 1),
                                  GuessedAt = guess.CreatedAt
                              };

            return await leaderboard
                .OrderBy(x => x.ErrorMeters)
                .ThenBy(x => x.GuessedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
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