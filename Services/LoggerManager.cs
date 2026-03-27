using System;
using System.IO;
using PictureView.Helpers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PictureView.Services;

public class MemoryEventSink : ILogEventSink
{
    /**
     * 当有日志产生时触发，传出 时间、级别、消息
     */
    public event Action<DateTime, string, string>? OnLogEmitted;

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        var level = logEvent.Level.ToString();
        OnLogEmitted?.Invoke(logEvent.Timestamp.DateTime, level, message);
    }
}

public static class LoggerManager
{
    public const string LogsFolderName = "logs";
    public const string LogsFileName = "app_log.txt";
    public static MemoryEventSink EventSink { get; } = new();

    public static void Initialize()
    {
        // 获取当前缓存目录，拼出 logs 文件夹
        var logDir = Path.Combine(AppDataManager.GetActiveCacheDirectory(), LogsFolderName);
        var logFilePath = Path.Combine(logDir, LogsFileName);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                // 10MB
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 5,
                shared: true
            )
            .WriteTo.Sink(EventSink)
            .CreateLogger();

        Log.Information("PictureView 日志系统初始化成功！");
    }

    public static void Close()
    {
        Log.CloseAndFlush();
    }
    
    public static void Reconfigure()
    {
        // 关闭旧文件句柄
        Log.CloseAndFlush();
        Initialize();
    }
}