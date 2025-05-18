using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace geotagger_backend.Models
{
    [Table("GeoPasswordResetTokens")]
    public class GeoPasswordResetToken
    {
        [Key]
        [Column("ResetId")]
        public long ResetId { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
