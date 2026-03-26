using System.Diagnostics;
using System.Text.Json;

namespace PictureView.Utils;

public static class DebugHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Dump(this object? obj, string message = "")
    {
        if (obj == null)
        {
            Debug.WriteLine($"{message} null");
            return;
        }
        
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        Debug.WriteLine(string.IsNullOrEmpty(message) ? json : $"--- {message} ---\n{json}");
    }
}