namespace geotagger_backend.DTOs
{
    public class UserGuessDto
    {
        public int LocationId { get; set; }
        public double ErrorMeters { get; set; }
        public string ImageUrl { get; set; }
    }

}
