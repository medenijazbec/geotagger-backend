namespace geotagger_backend.DTOs
{
    public class GeoUserActionLogDto
    {
        public string? UserId { get; set; } // fallback, but use JWT if possible!
        public string ActionType { get; set; }
        public string? ComponentType { get; set; }
        public string? NewValue { get; set; }
        public string Url { get; set; }
        // ActionTimestamp is NOT sent by frontend, set on server
    }

}
