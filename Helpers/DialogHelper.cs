using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace PictureView.Helpers;

public static class DialogHelper
{
    public static async Task<string[]> OpenFolderPicker(Control sourceControl)
    {
        var topLevel = TopLevel.GetTopLevel(sourceControl);
        if (topLevel == null) return [];

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择图片文件夹",
            AllowMultiple = true
        });

        if (folders.Count == 0) return [];

        return folders
            .Select(f => f.TryGetLocalPath())
            .Where(path => !string.IsNullOrEmpty(path))
            .Cast<string>()
            .ToArray();
    }
}