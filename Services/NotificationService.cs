using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using geotagger_backend.Data;
using geotagger_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace geotagger_backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        public NotificationService(ApplicationDbContext db) => _db = db;

        public async Task CreateAsync(Notification n)
        {
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();
        }
        /// <summary>
        /// Retrieves notifications for a specific user, ordered by most recent first.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>
        /// A task that returns an enumerable of <see cref="Notification"/>.
        /// </returns>
        public Task<IEnumerable<Notification>> GetForUserAsync(string userId) => _db.Notifications
          .Where(n => n.UserId == userId)
          .OrderByDescending(n => n.Timestamp)
          .AsNoTracking()
          .ToListAsync()
          .ContinueWith(t => t.Result.AsEnumerable());
        /// <summary>
        /// Marks a specific notification as read if it belongs to the user and is not already read.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <param name="notificationId">The identifier of the notification to mark as read.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task MarkAsReadAsync(string userId, int notificationId)
        {
            var n = await _db.Notifications
              .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);
            if (n != null && !n.IsRead)
            {
                n.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }
        /// <summary>
        /// Marks all unread notifications for a specific user as read.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _db.Notifications
              .Where(n => n.UserId == userId && !n.IsRead)
              .ToListAsync();

            notifications.ForEach(n => n.IsRead = true);

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves the count of unread notifications for a specific user.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>A task that returns the number of unread notifications.</returns>
        public Task<int> GetUnreadCountAsync(string userId) => _db.Notifications
          .Where(n => n.UserId == userId && !n.IsRead)
          .CountAsync();
    }
}