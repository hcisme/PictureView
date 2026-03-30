using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using PictureView.Helpers;
using PictureView.Models;
using Serilog;

namespace PictureView.Services;

public static class FolderDataManager
{
    private static readonly Lock FileLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string GetFoldersJsonPath()
        => Path.Combine(AppConfigManager.GetActiveCacheDirectory(), Constant.FoldersFileName);

    public static List<FolderModel> LoadFolders()
    {
        var path = GetFoldersJsonPath();

        lock (FileLock)
        {
            if (!File.Exists(path))
            {
                Log.Information("文件夹列表缓存文件不存在，返回空列表。");
                return [];
            }

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return [];
                }

                var folders = JsonSerializer.Deserialize<List<FolderModel>>(json);
                Log.Information("加载文件夹列表成功。");
                return folders ?? [];
            }
            catch (JsonException jsonEx)
            {
                Log.Error(jsonEx, "文件夹缓存文件 JSON 格式损坏");
                return [];
            }
            catch (Exception e)
            {
                Log.Error(e, "读取缓存文件夹列表时发生未知错误");
                return [];
            }
        }
    }

    public static void SaveFolders(IEnumerable<FolderModel> folders)
    {
        var path = GetFoldersJsonPath();
        var tempPath = path + ".tmp";

        lock (FileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(folders, JsonOptions);
                // 安全写入
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, path, overwrite: true);
                Log.Information("保存文件夹列表成功。");
            }
            catch (Exception e)
            {
                Log.Error(e, "保存文件夹列表失败");
            }
        }
    }
}