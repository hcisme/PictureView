using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    // 右侧的列表 以及操作按钮相关
    private readonly Dictionary<string, List<ImageItemViewModel>> _folderImageCache = new();
    public ObservableCollection<ImageItemViewModel> ImageItems { get; } = [];
    public bool HasSelection => ImageItems.Any(img => img.IsSelected);
    [ObservableProperty] private bool _showRealImages;

    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public MainWindowViewModel()
    {
        // 获取 设置 相关对象信息
        CurrentCacheLocation = AppConfigManager.GetActiveCacheDirectory();
        EnableProxy = AppConfigManager.CurrentConfig.EnableProxy;
        ProxyHost = AppConfigManager.CurrentConfig.ProxyHost;
        ProxyPort = AppConfigManager.CurrentConfig.ProxyPort;
    }

    public override async Task OnAppearingAsync()
    {
        LoggerManager.EventSink.OnLogEmitted += HandleNewLog;
        await AsyncReadFolderList();
    }

    public override Task OnDisappearingAsync()
    {
        LoggerManager.EventSink.OnLogEmitted -= HandleNewLog;
        return Task.CompletedTask;
    }

    private async Task AsyncReadFolderList()
    {
        try
        {
            var savedFolders = await Task.Run(FolderDataManager.LoadFolders);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FolderList.Clear();
                foreach (var folder in savedFolders)
                {
                    FolderList.Add(new FolderItemModel(id: folder.Id, fullPath: folder.Path));
                }

                ApplyFilter();
                OnPropertyChanged(nameof(HasData));
            });
        }
        catch (Exception error)
        {
            Log.Error(error, "初始化加载文件夹列表时发生错误。");
        }
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
        )).ToList();
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
        ImageItems.Clear();
        if (value == null) return;

        try
        {
            var folderId = value.Id;

            if (_folderImageCache.TryGetValue(folderId, out var cachedImages))
            {
                foreach (var img in cachedImages)
                {
                    if (ShowRealImages) _ = img.LoadThumbnailAsync();
                    ImageItems.Add(img);
                }
            }
            else
            {
                var files = Directory.EnumerateFiles(value.FullPath)
                    .Where(file =>
                        {
                            if (!_allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                                return false;

                            var attributes = File.GetAttributes(file);
                            return (attributes & FileAttributes.Hidden) != FileAttributes.Hidden;
                        }
                    );
                foreach (var file in files)
                {
                    var item = new ImageItemViewModel(file, OnAnyImageSelectionChanged);
                    if (ShowRealImages) _ = item.LoadThumbnailAsync();
                    ImageItems.Add(item);
                }

                _folderImageCache[folderId] = ImageItems.ToList();
            }
        }
        catch (Exception error)
        {
            Log.Error(error, "在文件夹内查找文件失败");
        }
        finally
        {
            OnPropertyChanged(nameof(HasSelection));
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
        _folderImageCache.Remove(folderToRemove.Id);
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

    // 全选
    public void SelectAll()
    {
        foreach (var item in ImageItems) item.IsSelected = true;
        OnPropertyChanged(nameof(HasSelection));
    }

    // 反选
    public void InvertSelection()
    {
        foreach (var item in ImageItems) item.IsSelected = !item.IsSelected;
        OnPropertyChanged(nameof(HasSelection));
    }

    // 伪删除
    public void DeleteSelected()
    {
        var itemsToRemove = ImageItems.Where(x => x.IsSelected).ToList();
        var currentFolderId = SelectedFolder?.Id;

        foreach (var item in itemsToRemove)
        {
            try
            {
                if (File.Exists(item.FilePath))
                {
                    var attributes = File.GetAttributes(item.FilePath);
                    File.SetAttributes(item.FilePath, attributes | FileAttributes.Hidden);
                }

                ImageItems.Remove(item);

                if (currentFolderId != null && _folderImageCache.TryGetValue(currentFolderId, out var cacheList))
                {
                    cacheList.Remove(item);
                }
            }
            catch
            {
                /* ignore */
            }
        }

        OnPropertyChanged(nameof(HasSelection));
    }

    // 导出
    public void ExportSelected()
    {
        var exportList = ImageItems.Where(x => x.IsSelected).Select(x => x.FilePath).ToList();
    }

    // F1 切换显示模式
    public void ToggleDisplayMode()
    {
        ShowRealImages = !ShowRealImages;

        if (!ShowRealImages) return;

        foreach (var item in ImageItems)
        {
            _ = item.LoadThumbnailAsync();
        }
    }

    private void OnAnyImageSelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
    }
}