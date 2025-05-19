namespace geotagger_backend.DTOs
{
    public record ActivityLogDto(
        long ActionId,
        string UserId,
        string? UserEmail,
        string? UserName,
        string ActionType,
        string? ComponentType,
        string? NewValue,
        string Url,
        DateTime ActionTimestamp
    );
}
