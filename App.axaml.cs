using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using PictureView.Helpers;
using PictureView.Services;
using PictureView.ViewModels;
using PictureView.Views;
using Serilog;

namespace PictureView;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetTrayIconTooltip();
        AppConfigManager.Initialize();
        LoggerManager.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 应用退出时的钩子
            desktop.Exit += OnExit;

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.MainWindow = _mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    
    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        WakeUpMainWindow();
    }
    
    private void OnShowMainWindowClicked(object? sender, EventArgs e)
    {
        WakeUpMainWindow();
    }
    
    private void WakeUpMainWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Show();
            
        // 如果最小化了 恢复成正常大小
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }
            
        // 强行把窗口拉到所有程序的最前面
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
    }
    
    private void OnExitAppClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
    
    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs args)
    {
        // 强制刷新缓冲区并释放所有日志文件的占用锁
        Log.Information("程序正常退出，释放日志文件句柄。");
        Log.CloseAndFlush();
    }

    private void SetTrayIconTooltip()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        
        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons is { Count: > 0 })
        {
            trayIcons[0].ToolTipText = $"{Constant.AppName} v{version}";
        }
    }
}