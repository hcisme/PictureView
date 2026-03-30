namespace PictureView.Models;

public class AppConfig
{
    /**
     * 文件缓存路径
     */
    public string? CacheLocation { get; set; }

    /**
     * 是否启用了代理
     */
    public bool EnableProxy { get; set; }
    
    /**
     * 代理ip
     */
    public string ProxyHost { get; set; } = "127.0.0.1";
    
    /**
     * 代理的端口号
     */
    public int ProxyPort { get; set; } = 10808;
}