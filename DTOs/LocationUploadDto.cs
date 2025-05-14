using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace geotagger_backend.DTOs
{
    public class LocationUploadDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Required] public IFormFile Image { get; set; } = null!;

    }

}
