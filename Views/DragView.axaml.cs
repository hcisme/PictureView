using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PictureView.Helpers;
using PictureView.ViewModels;
using Serilog;

namespace PictureView.Views;

public partial class DragView : UserControl
{
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public DragView()
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
            .Cast<string>()
            .ToArray();

        if (folderPaths.Length > 0 && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddFolders(folderPaths);
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
            var vm = ViewModel;
            if (vm == null) return;
            
            var folderPaths = await DialogHelper.OpenFolderPicker(this);
            if (folderPaths.Length > 0)
            {
                vm.AddFolders(folderPaths);
            }
        }
        catch (Exception error)
        {
            Log.Error(error, "添加图片文件夹失败");
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