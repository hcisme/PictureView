using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using PictureView.Models;

namespace PictureView.Helpers;

public static class AppDataManager
{
    private const string PictureView = "PictureView";
    private const string MasterConfigFileName = "config.json";
    public const string FoldersFileName = "folders.json";

    /**
     * 永远不变的主配置目录: C:\Users\xxx\AppData\Roaming\PictureView
     */
    private static readonly string RoamingDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PictureView);

    /**
     * 主配置文件路径
     */
    private static readonly string MasterConfigPath = Path.Combine(RoamingDir, MasterConfigFileName);

    /**
     * 内存中缓存的主配置
     */
    public static AppConfig CurrentConfig { get; private set; } = new();

    /**
     * JSON 序列化配置
     */
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /**
     * 初始化配置
     */
    public static void Initialize()
    {
        Directory.CreateDirectory(RoamingDir);

        if (File.Exists(MasterConfigPath))
        {
            try
            {
                var jsonString = File.ReadAllText(MasterConfigPath);
                CurrentConfig = JsonSerializer.Deserialize<AppConfig>(jsonString) ?? new AppConfig();
            }
            catch
            {
                /* 如果解析失败，保留默认值 */
            }
        }
        else
        {
            // 初次运行，生成默认配置
            SaveConfig();
        }
    }

    public static void SaveConfig()
    {
        Directory.CreateDirectory(RoamingDir);
        var json = JsonSerializer.Serialize(CurrentConfig, JsonOptions);
        File.WriteAllText(MasterConfigPath, json);
    }

    /**
     * 获取真实的数据缓存目录
     */
    public static string GetActiveCacheDirectory()
    {
        if (CurrentConfig.CacheLocation == "default" || string.IsNullOrWhiteSpace(CurrentConfig.CacheLocation))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                PictureView
            );
        }

        return CurrentConfig.CacheLocation;
    }
    
    private static string GetFoldersJsonPath() => Path.Combine(GetActiveCacheDirectory(), FoldersFileName);

    public static List<FolderModel> LoadFolders()
    {
        var path = GetFoldersJsonPath();
        if (!File.Exists(path)) return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<FolderModel>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void SaveFolders(IEnumerable<FolderModel> folders)
    {
        var cacheDir = GetActiveCacheDirectory();
        // 确保当前工作目录存在
        Directory.CreateDirectory(cacheDir);

        var json = JsonSerializer.Serialize(folders, JsonOptions);
        File.WriteAllText(GetFoldersJsonPath(), json);
    }
}