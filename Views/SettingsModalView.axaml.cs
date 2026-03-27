using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PictureView.Helpers;
using PictureView.Services;
using PictureView.ViewModels;
using Serilog;

namespace PictureView.Views;

public partial class SettingsModalView : UserControl
{
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public SettingsModalView()
    {
        InitializeComponent();
    }

    // 点击 X 按钮关闭 Modal
    private void OnCloseSettingsButtonClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel?.IsSettingsOpen = false;
    }

    private async void OnChangeCacheLocationClicked(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择新的缓存数据存放目录",
                AllowMultiple = false
            });

            if (folders.Count <= 0) return;
            var newCacheDir = folders[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(newCacheDir)) return;

            var oldCacheDir = AppDataManager.GetActiveCacheDirectory();
            if (oldCacheDir.Equals(newCacheDir, StringComparison.OrdinalIgnoreCase) ||
                newCacheDir.StartsWith(oldCacheDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               ) return;

            vm.IsLoading = true;
            Log.Information("准备将缓存目录从 {OldCacheDir} 迁移至 {NewCacheDir}", oldCacheDir, newCacheDir);

            // 闭日志写入
            LoggerManager.Close();

            await Task.Run(() =>
            {
                Thread.Sleep(2000);
                Directory.CreateDirectory(newCacheDir);

                if (Directory.Exists(oldCacheDir))
                {
                    try
                    {
                        foreach (var dirPath in Directory.GetDirectories(oldCacheDir, "*", SearchOption.AllDirectories))
                        {
                            var relativePath = dirPath[(oldCacheDir.Length + 1)..];
                            Directory.CreateDirectory(Path.Combine(newCacheDir, relativePath));
                        }

                        foreach (var filePath in Directory.GetFiles(oldCacheDir, "*.*", SearchOption.AllDirectories))
                        {
                            var relativePath = filePath[(oldCacheDir.Length + 1)..];
                            var targetFilePath = Path.Combine(newCacheDir, relativePath);
                            File.Copy(filePath, targetFilePath, overwrite: true);
                        }

                        // 写入配置文件
                        AppDataManager.CurrentConfig.CacheLocation = newCacheDir;
                        AppDataManager.SaveConfig();
                    }
                    catch (Exception)
                    {
                        if (Directory.Exists(newCacheDir))
                        {
                            Directory.Delete(newCacheDir, true);
                        }

                        throw;
                    }

                    try
                    {
                        // 尝试删掉旧缓存目录
                        Directory.Delete(oldCacheDir, recursive: true);
                    }
                    catch
                    {
                        // ignore
                    }
                }
                else
                {
                    // 如果原来压根没老目录，直接存新地址
                    AppDataManager.CurrentConfig.CacheLocation = newCacheDir;
                    AppDataManager.SaveConfig();
                }
            });

            // 重新接管日志
            LoggerManager.Initialize();
            vm.CurrentCacheLocation = AppDataManager.GetActiveCacheDirectory();
            Log.Information("缓存数据已成功迁移并生效");
        }
        catch (Exception ex)
        {
            LoggerManager.Initialize();
            Log.Error(ex, "更改缓存位置失败，已执行事务回滚，原目录未破坏。");
        }
        finally
        {
            vm.IsLoading = false;
        }
    }

    private void OnOpenCacheLocationClicked(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null) return;
        var cachePath = vm.CurrentCacheLocation;

        try
        {
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            Process.Start(new ProcessStartInfo
                {
                    FileName = cachePath,
                    UseShellExecute = true
                }
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开缓存文件夹失败");
        }
    }
}