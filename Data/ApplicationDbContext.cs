using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using geotagger_backend.Models;

namespace geotagger_backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{Auction}"/> representing auctions in the system.
        /// </summary>
        public DbSet<Auction> Auctions { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="DbSet{Bid}"/> representing bids placed on auctions.
        /// </summary>
        public DbSet<Bid> Bids { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="DbSet{Notification}"/> representing notifications sent to users.
        /// </summary>
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

        }
    }
}
