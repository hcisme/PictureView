using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PictureView.Helpers;
using PictureView.ViewModels;

namespace PictureView.Views;

public partial class SettingsModalView : UserControl
{
    public SettingsModalView()
    {
        InitializeComponent();
    }

    // 点击 X 按钮关闭 Modal
    private void OnCloseSettingsButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsSettingsOpen = false;
        }
    }

    private async void OnChangeCacheLocationClicked(object? sender, RoutedEventArgs e)
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

        if (DataContext is MainWindowViewModel viewModel)
        {
            var oldCacheDir = AppDataManager.GetActiveCacheDirectory();
            // 如果新老路径一样，或者新路径包含老路径（防止套娃死循环），直接拦截
            if (oldCacheDir.Equals(newCacheDir, StringComparison.OrdinalIgnoreCase) ||
                newCacheDir.StartsWith(
                    oldCacheDir + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase
                )
               ) return;

            try
            {
                viewModel.IsLoading = true;
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(newCacheDir);

                    if (Directory.Exists(oldCacheDir))
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
                            if (File.Exists(targetFilePath)) File.Delete(targetFilePath);
                            File.Move(filePath, targetFilePath);
                        }
                    }

                    // 存盘
                    AppDataManager.CurrentConfig.CacheLocation = newCacheDir;
                    AppDataManager.SaveConfig();
                });

                // 更新 UI 上的文本框显示
                viewModel.CurrentCacheLocation = AppDataManager.GetActiveCacheDirectory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"数据搬家失败: {ex.Message}");
            }
            finally
            {
                viewModel.IsLoading = false;
            }
        }
    }

    private void OnOpenCacheLocationClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var cachePath = viewModel.CurrentCacheLocation;

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
                // 如果遇到权限问题等，打印日志
                Debug.WriteLine($"无法打开目录: {ex.Message}");
            }
        }
    }
}