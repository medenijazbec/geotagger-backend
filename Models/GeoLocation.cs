using System;
using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.Models
{
    /// <summary>
    /// Ground‑truth photo with coordinates.
    /// </summary>
    public class GeoLocation
    {
        [Key] public int LocationId { get; set; }

        [Required] public string UploaderId { get; set; } = null!; // FK → GeoUser

        [Required][MaxLength(512)] public string S3OriginalKey { get; set; } = null!;
        [MaxLength(512)] public string? S3ThumbnailKey { get; set; }

        [MaxLength(255)] public string? Title { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        /* navigation */
        public GeoUser? Uploader { get; set; }
        public ICollection<GeoGuess> Guesses { get; set; } = new List<GeoGuess>();
    }
}