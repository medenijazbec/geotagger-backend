namespace geotagger_backend.DTOs
{
    public record LocationListDto(
        int LocationId,
        string Title,
        string UploaderName,
        DateTime CreatedAt,
        bool IsActive);
}
