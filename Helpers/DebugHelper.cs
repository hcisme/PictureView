using System.Diagnostics;
using System.Text.Json;

namespace PictureView.Helpers;

public static class DebugHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Log(this object? obj, string tag = "")
    {
        if (obj == null)
        {
            Debug.WriteLine($"{tag} null");
            return;
        }
        
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        Debug.WriteLine(string.IsNullOrEmpty(tag) ? json : $"--- {tag} ---\n{json}");
    }
}