using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace geotagger_backend.Models
{
    [Table("GeoRefreshTokens")]
    public class RefreshToken
    {
        [Key]
        [Column("TokenId")]
        public long TokenId { get; set; }

        [Required]
        [Column("RefreshToken", TypeName = "char(88)")]
        public string Token { get; set; } = null!;

        [Required]
        [Column("ExpiresAt")]
        public DateTime ExpiresAt { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("RevokedAt")]
        public DateTime? RevokedAt { get; set; }

        [Column("ReplacedByToken", TypeName = "char(88)")]
        public string? ReplacedByToken { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }

}
