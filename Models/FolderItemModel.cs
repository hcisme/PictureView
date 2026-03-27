using System.IO;

namespace PictureView.Models;

public class FolderItemModel(string id, string fullPath)
{
    public string Id { get; } = id;
    public string FullPath { get; } = fullPath;
    public string Name => Path.GetFileName(FullPath); 
}