namespace geotagger_backend.DTOs
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Token { get; set; }      // existing JWT
        public string? RefreshToken { get; set; }   
    }


}
