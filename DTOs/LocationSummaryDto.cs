namespace geotagger_backend.DTOs
{
    public record LocationSummaryDto(
        int LocationId,
        string Title,
        bool IsActive);
}
