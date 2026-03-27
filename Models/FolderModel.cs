using System;

namespace PictureView.Models;

public class FolderModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Path { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.Now;
}