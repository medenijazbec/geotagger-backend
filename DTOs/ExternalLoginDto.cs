namespace geotagger_backend.DTOs
{
    public class ExternalLoginDto
    {
        public string Provider { get; set; } = string.Empty;   // "Google" or "Facebook"
        public string IdToken { get; set; } = string.Empty;   // Google: id_token, Facebook: access_token
    }
}
