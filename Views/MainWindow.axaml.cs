using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PictureView.ViewModels;

namespace PictureView.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 注册拖拽事件监听器
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        // 监听拖拽进入和离开
        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DragLeaveEvent, DragLeave);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        var storageItems = e.DataTransfer.TryGetFiles();
        var isFolderOnly = AreAllFolders(storageItems);
        e.DragEffects = isFolderOnly ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        DropZoneBorder.Classes.Remove("dragover");
        if (!e.DataTransfer.Contains(DataFormat.File)) return;

        var storageItems = e.DataTransfer.TryGetFiles();
        if (storageItems == null) return;
        if (!AreAllFolders(storageItems)) return;

        var folderPaths = storageItems
            .OfType<IStorageFolder>() // 只取文件夹
            .Select(folder => folder.TryGetLocalPath()) // 转换为真实路径字符串
            .Where(path => !string.IsNullOrEmpty(path)) // 过滤掉无效路径
            .ToArray();

        if (folderPaths.Length > 0 && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddFolders(folderPaths!);
        }
    }

    // 拖拽进入时：加上高亮类
    private void DragEnter(object? sender, DragEventArgs e)
    {
        if (AreAllFolders(e.DataTransfer.TryGetFiles()))
        {
            DropZoneBorder.Classes.Add("dragover");
        }
    }

    // 拖拽离开时：移除高亮类
    private void DragLeave(object? sender, RoutedEventArgs e)
    {
        DropZoneBorder.Classes.Remove("dragover");
    }

    private async void OnDropZonePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            await OpenFolderPickerAndAddAsync();
        }
        catch
        {
            // ignored
        }
    }

    private async void OnAddFolderButtonClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            await OpenFolderPickerAndAddAsync();
        }
        catch
        {
            // ignored
        }
    }

    // 提取出来的公共选择文件夹逻辑
    private async Task OpenFolderPickerAndAddAsync()
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "选择图片文件夹",
                    AllowMultiple = true
                }
            );

            if (folders.Count <= 0) return;
            var folderPaths = folders
                .Select(f => f.TryGetLocalPath())
                .Where(path => !string.IsNullOrEmpty(path))
                .ToArray();

            if (folderPaths.Length > 0 && DataContext is MainWindowViewModel viewModel)
            {
                viewModel.AddFolders(folderPaths!);
            }
        }
        catch (Exception error)
        {
            Debug.WriteLine(error.Message);
        }
    }

    /**
     * 是否全是文件夹
     */
    private static bool AreAllFolders(IReadOnlyList<IStorageItem>? items)
    {
        return items is { Count: > 0 } && items.All(item => item is IStorageFolder);
    }
}