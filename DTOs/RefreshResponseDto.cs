﻿namespace geotagger_backend.DTOs
{
    public class RefreshResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
