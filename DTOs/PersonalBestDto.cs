namespace geotagger_backend.DTOs
{
    public class PersonalBestDto
    {
        public int LocationId { get; set; }
        public double ErrorMeters { get; set; }
        public string ImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
