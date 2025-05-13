// File: Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using geotagger_backend.Models;

namespace geotagger_backend.Data
{
    /// <summary>
    /// Connects ASP-NET Core Identity *and* the Geo-game entities to an
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── Geo-game tables (match mySQL schema) ─────────

        public DbSet<GeoUser> GeoUsers => Set<GeoUser>();
        public DbSet<GeoLocation> GeoLocations => Set<GeoLocation>();
        public DbSet<GeoGuess> GeoGuesses => Set<GeoGuess>();
        public DbSet<GeoPointsTransaction> GeoPointsTransactions => Set<GeoPointsTransaction>();
        public DbSet<GeoUserActionLog> GeoUserActionLogs => Set<GeoUserActionLog>();

        public DbSet<Notification> Notifications => Set<Notification>();

        // ── Fluent mappings (only relationships; no schema tweaking) ─────────
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // GeoUser Identity one-to-one
            builder.Entity<GeoUser>()
                   .HasOne<ApplicationUser>()
                   .WithOne()
                   .HasForeignKey<GeoUser>(g => g.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // GeoLocation GeoUser
            builder.Entity<GeoLocation>()
                   .HasOne(l => l.Uploader)
                   .WithMany(u => u.Locations)
                   .HasForeignKey(l => l.UploaderId);

            // GeoGuess GeoLocation & GeoUser
            builder.Entity<GeoGuess>()
                   .HasOne(g => g.Location)
                   .WithMany(l => l.Guesses)
                   .HasForeignKey(g => g.LocationId);

            builder.Entity<GeoGuess>()
                   .HasOne(g => g.User)
                   .WithMany(u => u.Guesses)
                   .HasForeignKey(g => g.UserId);
        }
    }
}
