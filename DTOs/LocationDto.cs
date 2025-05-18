namespace geotagger_backend.DTOs
{
    public class LocationDto // return to client
    {
        public int LocationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
