using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PictureView.Helpers;
using PictureView.Models;
using PictureView.Services;
using Serilog;

namespace PictureView.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // 全量数据源
    public ObservableCollection<FolderItemModel> FolderList { get; } = [];

    // 过滤后的数据源
    public ObservableCollection<FolderItemModel> FilteredFolders { get; } = [];

    // 搜索框绑定的文本
    [ObservableProperty] private string _searchText = string.Empty;

    public bool HasData => FolderList.Count > 0;

    // 选中的文件夹
    [ObservableProperty] private FolderItemModel? _selectedFolder;

    // 右侧的列表
    [ObservableProperty] private ObservableCollection<string> _dummyItems = [];
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    // 控制是否打开设置页面
    [ObservableProperty] private bool _isSettingsOpen;

    [ObservableProperty] private string _currentCacheLocation = string.Empty;

    // 控制全屏 Loading 遮罩
    [ObservableProperty] private bool _isLoading;

    // 任务栏
    [ObservableProperty] private string _statusMessage = "";
    // [ObservableProperty] private bool _isLogDrawerOpen;
    // public ObservableCollection<LogItemModel> LogHistory { get; } = [];

    // 代理状态绑定
    [ObservableProperty] private bool _enableProxy;
    [ObservableProperty] private string _proxyHost = "127.0.0.1";
    [ObservableProperty] private int _proxyPort = 10808;

    public MainWindowViewModel()
    {
        // 获取当前正在使用的缓存路径 UI显示
        CurrentCacheLocation = AppConfigManager.GetActiveCacheDirectory();
        EnableProxy = AppConfigManager.CurrentConfig.EnableProxy;
        ProxyHost = AppConfigManager.CurrentConfig.ProxyHost;
        ProxyPort = AppConfigManager.CurrentConfig.ProxyPort;

        // 从本地 JSON 读取曾经保存的文件夹
        var savedFolders = FolderDataManager.LoadFolders();
        foreach (var folder in savedFolders)
        {
            FolderList.Add(new FolderItemModel(id: folder.Id, fullPath: folder.Path));
        }

        ApplyFilter();

        LoggerManager.EventSink.OnLogEmitted += HandleNewLog;
    }

    public void AddFolders(string[] folderPaths)
    {
        var hasNew = false;
        foreach (var path in folderPaths)
        {
            if (FolderList.Any(f => f.FullPath == path)) continue;
            var newId = Guid.NewGuid().ToString();
            FolderList.Add(new FolderItemModel(id: newId, fullPath: path));
            hasNew = true;
        }

        if (!hasNew) return;

        ApplyFilter();
        OnPropertyChanged(nameof(HasData));

        var modelsToSave = FolderList.Select(f => new FolderModel(
            id: f.Id,
            path: f.FullPath,
            addedAt: DateTime.Now
        ));
        FolderDataManager.SaveFolders(modelsToSave);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    // 过滤
    private void ApplyFilter()
    {
        // 期望展示的文件夹
        var expectedFolders = FolderList.Where(f =>
            string.IsNullOrWhiteSpace(SearchText) ||
            f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        // 移除列表中不应该展示的项
        for (var i = FilteredFolders.Count - 1; i >= 0; i--)
        {
            if (!expectedFolders.Contains(FilteredFolders[i]))
            {
                FilteredFolders.RemoveAt(i);
            }
        }

        // 把缺失的项加回到列表中
        foreach (var folder in expectedFolders.Where(folder => !FilteredFolders.Contains(folder)))
        {
            FilteredFolders.Add(folder);
        }
    }

    partial void OnSelectedFolderChanged(FolderItemModel? value)
    {
        DummyItems.Clear();
        if (value == null) return;

        try
        {
            var fileCount = Directory.EnumerateFiles(value.FullPath)
                .Count(f => Enumerable.Contains(_allowedExtensions, Path.GetExtension(f).ToLower()));

            for (var i = 0; i < fileCount; i++)
            {
                DummyItems.Add($"占位图 {i + 1}");
            }
        }
        catch (Exception error)
        {
            Debug.WriteLine(error.Message);
        }
    }

    private void HandleNewLog(DateTime time, string level, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = message;
            // LogHistory.Insert(0, new LogItemModel(time, level, message));
            // // 限制内存，防止程序挂机几个月内存撑爆 (最多保留 200 条界面记录)
            // if (LogHistory.Count > 200)
            // {
            //     LogHistory.RemoveAt(LogHistory.Count - 1);
            // }
        });
    }

    partial void OnEnableProxyChanged(bool value) => SaveProxyConfig();
    partial void OnProxyHostChanged(string value) => SaveProxyConfig();
    partial void OnProxyPortChanged(int value) => SaveProxyConfig();

    private void SaveProxyConfig()
    {
        AppConfigManager.CurrentConfig.EnableProxy = EnableProxy;
        AppConfigManager.CurrentConfig.ProxyHost = ProxyHost;
        AppConfigManager.CurrentConfig.ProxyPort = ProxyPort;
        AppConfigManager.SaveConfig();
    }

    public void RemoveFolder(FolderItemModel folderToRemove)
    {
        if (SelectedFolder == folderToRemove)
        {
            SelectedFolder = null;
        }
        FolderList.Remove(folderToRemove);
        ApplyFilter();
        OnPropertyChanged(nameof(HasData));

        var modelsToSave = FolderList.Select(f =>
            new FolderModel(
                id: f.Id,
                path: f.FullPath,
                addedAt: DateTime.Now
            )
        ).ToList();

        FolderDataManager.SaveFolders(modelsToSave);
        Log.Information("已移除文件夹: {Path}", folderToRemove.FullPath);
    }
}