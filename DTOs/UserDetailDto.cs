namespace geotagger_backend.DTOs
{
    public record UserDetailDto(
        string Id,
        string Email,
        string FirstName,
        string LastName,
        string? ProfilePictureUrl,
        IEnumerable<LocationSummaryDto> Locations);
}
