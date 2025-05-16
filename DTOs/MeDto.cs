namespace geotagger_backend.DTOs
{
    public record MeDto(
        string Id,
        string FirstName,
        string LastName,
        string Email,
        string? ProfilePictureUrl,
        bool IsExternal        
        );

}
