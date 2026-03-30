using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PictureView.Helpers;
using PictureView.Services;
using PictureView.ViewModels;
using Serilog;

namespace PictureView.Views;

public partial class TaskbarView : UserControl
{
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public TaskbarView()
    {
        InitializeComponent();
    }

    /**
     * 打开设置 Modal
     */
    private void OnSettingsButtonClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel?.IsSettingsOpen = true;
    }

    private void OnStatusLogClicked(object? sender, RoutedEventArgs e)
    {
        var logDir = Path.Combine(AppConfigManager.GetActiveCacheDirectory(), LoggerManager.LogsFolderName);
        if (!Directory.Exists(logDir)) return;

        var latestLogFile = Directory.GetFiles(logDir, "app_log*.txt")
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(latestLogFile))
        {
            // 如果 logs 文件夹是空的，就只打开文件夹
            Process.Start(new ProcessStartInfo
            {
                FileName = logDir,
                UseShellExecute = true
            });
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Windows 调用 explorer.exe 加上 /select
                Process.Start("explorer.exe", $"/select,\"{latestLogFile}\"");
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Mac 调用 open -R 命令
                Process.Start("open", $"-R \"{latestLogFile}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo
                    {
                        FileName = logDir,
                        UseShellExecute = true
                    }
                );
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "无法打开日志文件");
        }
    }
}