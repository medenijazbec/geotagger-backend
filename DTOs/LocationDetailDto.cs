namespace geotagger_backend.DTOs
{
    public record LocationDetailDto(
        int LocationId,
        string Title,
        string Description,
        double Latitude,
        double Longitude,
        bool IsActive,
        DateTime CreatedAt,
        string UploaderId,
        string UploaderName,
        string S3OriginalKey,
        string? S3ThumbnailKey);
}
