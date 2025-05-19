namespace geotagger_backend.DTOs
{
    public class ClientActionLogDto
    {
        public string? UserId { get; set; }           // Optional: null if not logged in
        public string ActionType { get; set; }        // "click", "scroll", etc.
        public string? ComponentType { get; set; }
        public string? NewValue { get; set; }
        public string Url { get; set; }
        public DateTime? ActionTimestamp { get; set; }
    }
}
