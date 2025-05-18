using System;
using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.Models
{
    public class GeoGuess
    {
        [Key] public int GuessId { get; set; }
        [Required] public int LocationId { get; set; }
        [Required] public string UserId { get; set; } = null!;

        [Range(-90, 90)] public decimal Latitude { get; set; }
        [Range(-180, 180)] public decimal Longitude { get; set; }

        public double ErrorMeters { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /* navigation */
        public GeoLocation? Location { get; set; }
        public GeoUser? User { get; set; }
    }
}