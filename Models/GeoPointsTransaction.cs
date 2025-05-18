using System;
using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.Models
{
    public enum PointsReason { registration_bonus, upload_reward, guess_cost }

    public class GeoPointsTransaction
    {
        [Key] public long TransactionId { get; set; }
        [Required] public string UserId { get; set; } = null!;
        public int? LocationId { get; set; }
        public int? GuessId { get; set; }
        public int PointsDelta { get; set; }
        public PointsReason Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /* navigation */
        public GeoUser? User { get; set; }
        public GeoLocation? Location { get; set; }
        public GeoGuess? Guess { get; set; }
    }
}
