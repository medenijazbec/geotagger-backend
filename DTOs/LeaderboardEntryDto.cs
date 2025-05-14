namespace geotagger_backend.DTOs
{
    public class LeaderboardEntryDto
    {
        public string UserId { get; set; } = null!;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public double ErrorMeters { get; set; }
        public DateTime GuessedAt { get; set; }
    }
}
