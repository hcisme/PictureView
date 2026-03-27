using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PictureView.Helpers;
using PictureView.Models;

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

    public MainWindowViewModel()
    {
        // 获取当前正在使用的缓存路径 UI显示
        CurrentCacheLocation = AppDataManager.GetActiveCacheDirectory();

        // 从本地 JSON 读取曾经保存的文件夹
        var savedFolders = AppDataManager.LoadFolders();
        foreach (var folder in savedFolders)
        {
            FolderList.Add(new FolderItemModel(folder.Path));
        }

        ApplyFilter();
    }

    public void AddFolders(string[] folderPaths)
    {
        var hasNew = false;
        foreach (var path in folderPaths)
        {
            if (FolderList.Any(f => f.FullPath == path)) continue;
            FolderList.Add(new FolderItemModel(path));
            hasNew = true;
        }

        if (!hasNew) return;
        ApplyFilter();
        OnPropertyChanged(nameof(HasData));
        var modelsToSave = FolderList.Select(f => new FolderModel { Path = f.FullPath });
        AppDataManager.SaveFolders(modelsToSave);
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
}