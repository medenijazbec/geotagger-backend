namespace geotagger_backend.Models;

public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public bool UseSsl { get; set; }
    public string From { get; set; } = "";
}
