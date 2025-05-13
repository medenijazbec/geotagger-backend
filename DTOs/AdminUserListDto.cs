namespace geotagger_backend.DTOs.Admin
{
    public class AdminUserListDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ProfilePictureUrl { get; set; } = null!;
    }
}
