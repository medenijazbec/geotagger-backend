namespace geotagger_backend.DTOs
{
    public record UpdateLocationDto(
        string Title,
        string Description,
        double Latitude,
        double Longitude,
        bool IsActive);
}
