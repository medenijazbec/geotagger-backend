namespace geotagger_backend.DTOs
{
    public class GuessResultDto
    {
        public int LocationId { get; set; }
        public double ErrorMeters { get; set; }
        public int AttemptNumber { get; set; }
        public int RemainingPoints { get; set; }
        public int AwardedPoints { get; set; } 
    }

}
