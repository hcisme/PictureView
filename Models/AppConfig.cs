namespace PictureView.Models;

public class AppConfig
{
    public string? CacheLocation { get; set; }
    
    public bool EnableProxy { get; set; } = false;
    public string ProxyHost { get; set; } = "127.0.0.1";
    public int ProxyPort { get; set; } = 10808;
}