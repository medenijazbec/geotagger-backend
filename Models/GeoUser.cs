using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace geotagger_backend.Models
{
    /// <summary>
    /// Extends Identity user with in‑game wallet & statistics.
    /// Row is created when the Identity user is created.
    /// </summary>
    public class GeoUser
    {
        [Key]
        public string UserId { get; set; } = null!;   // same as AspNetUsers.Id

        public int GamePoints { get; set; } = 10;  // initial bonus
        public int TotalLocationsUploaded { get; set; }
        public int TotalGuessesMade { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /* navigation */
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? Identity { get; set; }
        public ICollection<GeoLocation> Locations { get; set; } = new List<GeoLocation>();
        public ICollection<GeoGuess> Guesses { get; set; } = new List<GeoGuess>();
    }
}
