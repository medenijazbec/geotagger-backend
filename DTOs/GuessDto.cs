using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public class GuessDto
    {
        [Required] public int LocationId { get; set; }
        [Required] public decimal Latitude { get; set; }
        [Required] public decimal Longitude { get; set; }
    }
}