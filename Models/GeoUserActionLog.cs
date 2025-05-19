using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace geotagger_backend.Models
{
    [Table("GeoUserActionLogs")]
    public class GeoUserActionLog
    {
        [Key]
        public long ActionId { get; set; }

        [Required]
        public string UserId { get; set; } = null!; // foreign key to GeoUser

        [ForeignKey(nameof(UserId))]
        public GeoUser? GeoUser { get; set; } // navigation to GeoUser

        // Add your other columns:
        public string ActionType { get; set; } = null!;
        public string? ComponentType { get; set; }
        public string? NewValue { get; set; }
        public string Url { get; set; } = null!;

        public DateTime ActionTimestamp { get; set; }
    }

}
