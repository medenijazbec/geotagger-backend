using System.ComponentModel.DataAnnotations;

namespace geotagger_backend.DTOs
{
    public record UserSummaryDto(
        string Id,
        [EmailAddress] string Email,
        string FirstName,
        string LastName,
        string? ProfilePictureUrl);
}
