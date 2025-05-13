using System;

namespace geotagger_backend.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string UserId { get; set; } = null!;     // FK to AspNetUsers.Id
        public int AuctionId { get; set; }              // which auction
        public string Kind { get; set; } = null!;       // "outbid", "bid-finished", "my-finished"
        public string Title { get; set; } = null!;      // auction title
        public DateTime Timestamp { get; set; }         // when it happened
        public bool IsRead { get; set; } = false;
    }
}
