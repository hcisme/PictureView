using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using PictureView.Models;
using Serilog;

namespace PictureView.Helpers;

public static class AppConfigManager
{
    private static readonly string RoamingDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constant.AppName);

    /**
     *  主配置文件路径
     */
    private static readonly string MasterConfigPath = Path.Combine(RoamingDir, Constant.MasterConfigFileName);

    /**
     * 用于文件读写的线程锁
     */
    private static readonly Lock FileLock = new();

    /**
     * 当前的应用配置
     */
    public static AppConfig CurrentConfig { get; private set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static void Initialize()
    {
        Directory.CreateDirectory(RoamingDir);

        lock (FileLock)
        {
            if (File.Exists(MasterConfigPath))
            {
                try
                {
                    var jsonString = File.ReadAllText(MasterConfigPath);
                    CurrentConfig = JsonSerializer.Deserialize<AppConfig>(jsonString) ?? new AppConfig();
                    Log.Information("主配置文件已加载。");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "解析主配置文件失败，将备份原文件并使用默认配置。");
                    // 备份损坏的文件，防止用户辛辛苦苦配的设置直接被覆盖
                    var backupPath = $"{MasterConfigPath}.bak_{DateTime.Now:yyyyMMddHHmmss}";
                    File.Copy(MasterConfigPath, backupPath, true);

                    CurrentConfig = new AppConfig();
                    SaveConfigInternal();
                }
            }
            else
            {
                SaveConfigInternal();
            }
        }
    }

    public static void SaveConfig()
    {
        lock (FileLock)
        {
            SaveConfigInternal();
        }
    }

    public static string GetActiveCacheDirectory()
    {
        var path = string.IsNullOrWhiteSpace(CurrentConfig.CacheLocation)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Constant.AppName
            )
            : CurrentConfig.CacheLocation;

        Directory.CreateDirectory(path);
        return path;
    }

    // 内部实际保存逻辑，无锁
    private static void SaveConfigInternal()
    {
        Directory.CreateDirectory(RoamingDir);
        var json = JsonSerializer.Serialize(CurrentConfig, JsonOptions);

        // 安全写入 先写到临时文件，再覆盖
        var tempPath = MasterConfigPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, MasterConfigPath, overwrite: true);
    }
}