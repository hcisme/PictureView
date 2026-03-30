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

            var selectedBaseDir = folders[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(selectedBaseDir)) return;

            // 在用户选择的目录下，强制追加一个专属的缓存文件夹名
            var newCacheDir = Path.Combine(selectedBaseDir, Constant.AppName);
            var oldCacheDir = AppConfigManager.GetActiveCacheDirectory();

            // 防止目标等于源，或目标是源的子目录（防止无限递归套娃）
            if (oldCacheDir.Equals(newCacheDir, StringComparison.OrdinalIgnoreCase) ||
                newCacheDir.StartsWith(oldCacheDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            vm.IsLoading = true;
            Log.Information("准备将缓存目录从 {OldCacheDir} 迁移至 {NewCacheDir}", oldCacheDir, newCacheDir);
            LoggerManager.Close();

            await Task.Run(() =>
            {
                Thread.Sleep(2000);
                // 标记是否是本次操作新建的目录（用于安全回滚）
                var isNewDirCreatedByUs = false;

                try
                {
                    if (!Directory.Exists(newCacheDir))
                    {
                        Directory.CreateDirectory(newCacheDir);
                        isNewDirCreatedByUs = true;
                    }

                    if (Directory.Exists(oldCacheDir))
                    {
                        // 先把空文件夹 骨架建好
                        foreach (var dirPath in Directory.EnumerateDirectories(oldCacheDir, "*",
                                     SearchOption.AllDirectories))
                        {
                            var relativePath = Path.GetRelativePath(oldCacheDir, dirPath);
                            Directory.CreateDirectory(Path.Combine(newCacheDir, relativePath));
                        }

                        foreach (var filePath in Directory.EnumerateFiles(oldCacheDir, "*.*",
                                     SearchOption.AllDirectories))
                        {
                            var relativePath = Path.GetRelativePath(oldCacheDir, filePath);
                            var targetFilePath = Path.Combine(newCacheDir, relativePath);
                            File.Copy(filePath, targetFilePath, overwrite: true);
                        }
                    }

                    // 写入配置文件
                    AppConfigManager.CurrentConfig.CacheLocation = newCacheDir;
                    AppConfigManager.SaveConfig();

                    try
                    {
                        // 尝试删掉旧缓存目录
                        if (Directory.Exists(oldCacheDir))
                        {
                            Directory.Delete(oldCacheDir, recursive: true);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
                catch (Exception)
                {
                    if (isNewDirCreatedByUs && Directory.Exists(newCacheDir))
                    {
                        try
                        {
                            Directory.Delete(newCacheDir, true);
                        }
                        catch
                        {
                            /* ignore */
                        }
                    }

                    throw;
                }
            });

            // 重新接管日志
            LoggerManager.Initialize();
            vm.CurrentCacheLocation = AppConfigManager.GetActiveCacheDirectory();
            Log.Information("缓存数据已成功迁移并生效");
        }
        catch (Exception ex)
        {
            LoggerManager.Initialize();
            Log.Error(ex, "更改缓存位置失败。");
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