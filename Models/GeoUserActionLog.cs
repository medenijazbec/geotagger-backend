using System;
using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.Models
{
    public enum UserActionType { click, scroll, added_value, changed_value, removed_value }

    public class GeoUserActionLog
    {
        [Key] public long ActionId { get; set; }
        [Required] public string UserId { get; set; } = null!;
        public UserActionType ActionType { get; set; }
        public string? ComponentType { get; set; }
        public string? NewValue { get; set; }
        [Required, MaxLength(1024)] public string Url { get; set; } = null!;
        public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow;
    }
}
