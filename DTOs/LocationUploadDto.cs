using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace geotagger_backend.DTOs
{
    public class LocationUploadDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public decimal Latitude { get; set; }
        [Required] public decimal Longitude { get; set; }
        [Required] public IFormFile Image { get; set; } = null!;
    }

    public class LocationDto // return to client
    {
        public int LocationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
