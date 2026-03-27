using System;

namespace PictureView.Models;

public class FolderModel(string id, string path, DateTime addedAt)
{
    public string Id { get; set; } = id;
    public string Path { get; set; } = path;
    public DateTime AddedAt { get; set; } = addedAt;
}