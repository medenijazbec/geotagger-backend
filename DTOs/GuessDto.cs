using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public class GuessDto
    {
        [Required] public int LocationId { get; set; }
        [Required] public decimal Latitude { get; set; }
        [Required] public decimal Longitude { get; set; }
    }

    public class GuessResultDto
    {
        public int LocationId { get; set; }
        public double ErrorMeters { get; set; }
        public int AttemptNumber { get; set; }
        public int RemainingPoints { get; set; }
    }
}