using System.IO;

namespace PictureView.Models;

public class FolderItemModel(string fullPath)
{
    public string FullPath { get; } = fullPath;
    public string Name => Path.GetFileName(FullPath);
}