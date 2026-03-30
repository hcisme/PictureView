using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PictureView.Helpers;
using PictureView.Models;
using PictureView.ViewModels;
using Serilog;

namespace PictureView.Views;

public partial class ContentView : UserControl
{
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public ContentView()
    {
        InitializeComponent();
    }

    private async void OnAddFolderButtonClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folderPaths = await DialogHelper.OpenFolderPicker(this);
            if (folderPaths.Length > 0 && DataContext is MainWindowViewModel viewModel)
            {
                viewModel.AddFolders(folderPaths);
            }
        }
        catch (Exception error)
        {
            Log.Error(error, "添加图片文件夹失败");
        }
    }

    private async void OnCopyPathClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is MenuItem { DataContext: FolderItemModel folder })
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard == null) return;

                await topLevel.Clipboard.SetTextAsync(folder.FullPath);
                Log.Information("已复制路径到剪贴板");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "复制绝对路径失败。");
        }
    }

    private void OnOpenInExplorerClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: FolderItemModel folder })
        {
            try
            {
                Process.Start(new ProcessStartInfo
                    {
                        FileName = folder.FullPath,
                        UseShellExecute = true
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex, "打开文件夹失败。");
            }
        }
    }

    private void OnRemoveFolderClicked(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null) return;

        if (sender is MenuItem { DataContext: FolderItemModel folder })
        {
            vm.RemoveFolder(folder);
        }
    }
}