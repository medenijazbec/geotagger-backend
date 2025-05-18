// File: Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using geotagger_backend.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace geotagger_backend.Data
{
    /// <summary>
    /// ASP-NET Core Identity tables + Geo-game domain tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ───── DbSets ───────────────────────────────────────────────────────
        public DbSet<GeoUser> GeoUsers => Set<GeoUser>();
        public DbSet<GeoLocation> GeoLocations => Set<GeoLocation>();
        public DbSet<GeoGuess> GeoGuesses => Set<GeoGuess>();
        public DbSet<GeoPointsTransaction> GeoPointsTransactions => Set<GeoPointsTransaction>();
        public DbSet<GeoUserActionLog> GeoUserActionLogs => Set<GeoUserActionLog>();
        public DbSet<GeoPasswordResetToken> GeoPasswordResetTokens => Set<GeoPasswordResetToken>();

        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


        // ───── Fluent mappings (only relationships / column types) ─────────
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<GeoPointsTransaction>(entity =>
            {
                entity.Property(e => e.Reason)
                      .HasConversion(new EnumToStringConverter<PointsReason>())
                  
                      .HasColumnType("enum('registration_bonus','upload_reward','guess_cost')");
            });

            /* GeoUser 1-to-1 Identity */
            builder.Entity<GeoUser>()
                   .HasOne(g => g.Identity)
                   .WithOne()
                   .HasForeignKey<GeoUser>(g => g.UserId)
                   .HasPrincipalKey<ApplicationUser>(u => u.Id)
                   .OnDelete(DeleteBehavior.Cascade);

            /* GeoLocation ←→ GeoUser */
            builder.Entity<GeoLocation>()
                   .HasOne(l => l.Uploader)
                   .WithMany(u => u.Locations)
                   .HasForeignKey(l => l.UploaderId);

            /* GeoGuess relationships */
            builder.Entity<GeoGuess>()
                   .HasOne(g => g.Location)
                   .WithMany(l => l.Guesses)
                   .HasForeignKey(g => g.LocationId);

            builder.Entity<GeoGuess>()
                   .HasOne(g => g.User)
                   .WithMany(u => u.Guesses)
                   .HasForeignKey(g => g.UserId);

            /* FULL-PRECISION coordinates  (±90 / ±180 with 8-dp) */
            builder.Entity<GeoLocation>(e =>
            {
                // EF Core ≥ 5 : prefer HasPrecision; fallback to HasColumnType
#if NET5_0_OR_GREATER
                e.Property(p => p.Latitude).HasPrecision(10, 8);
                e.Property(p => p.Longitude).HasPrecision(11, 8);
#else
                e.Property(p => p.Latitude)  .HasColumnType("decimal(10,8)");
                e.Property(p => p.Longitude) .HasColumnType("decimal(11,8)");
#endif
            });
        }
    }
}
