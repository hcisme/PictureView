using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace PictureView.ViewModels;

public partial class ImageItemViewModel(string filePath, Action onSelectionChanged) : ViewModelBase
{
    [ObservableProperty] private bool _isSelected;
    public string FilePath { get; } = filePath;
    public string Name => Path.GetFileName(FilePath);
    [ObservableProperty] private Bitmap? _thumbnailImage;

    partial void OnIsSelectedChanged(bool value)
    {
        onSelectionChanged();
    }

    public async Task LoadThumbnailAsync()
    {
        if (ThumbnailImage != null) return;

        try
        {
            var bitmap = await Task.Run(() =>
                {
                    using var stream = File.OpenRead(FilePath);
                    return Bitmap.DecodeToWidth(stream, 200);
                }
            );
            Dispatcher.UIThread.Post(() => { ThumbnailImage = bitmap; });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "生成缩略图失败: {FilePath} -> {ExMessage}", FilePath, ex.Message);
        }
    }
}