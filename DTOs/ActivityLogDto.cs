namespace geotagger_backend.DTOs
{
    // DTO
    public record ActivityLogDto(
        long ActionId,
        string UserId,
        string? UserEmail,
        string? FirstName,
        string? LastName,
        string ActionType,
        string? ComponentType,
        string? NewValue,
        string Url,
        DateTime ActionTimestamp
    );

}
