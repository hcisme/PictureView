using System;

namespace PictureView.Models;

public class LogItemModel(DateTime time, string level, string message)
{
    public string TimeStr => time.ToString("HH:mm:ss");
    public string Level { get; } = level;
    public string Message { get; } = message;
}