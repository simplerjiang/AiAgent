using Microsoft.AspNetCore.Hosting;
using SimplerJiangAiAgent.Api.Infrastructure.Storage;

namespace SimplerJiangAiAgent.Api.Infrastructure.Logging;

public interface IFileLogWriter
{
    void Write(string category, string message);
}

public sealed class FileLogWriter : IFileLogWriter
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const int MaxBackups = 3;

    private readonly string _logFilePath;
    private readonly object _sync = new();

    public FileLogWriter(AppRuntimePaths runtimePaths)
    {
        var logDir = runtimePaths.LogsPath;
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "llm-requests.txt");
    }

    public void Write(string category, string message)
    {
        var safeCategory = string.IsNullOrWhiteSpace(category) ? "APP" : category.Trim();
        var safeMessage = message ?? string.Empty;
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{safeCategory}] {safeMessage}";
        lock (_sync)
        {
            RotateIfNeeded();
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath)) return;
        var fi = new FileInfo(_logFilePath);
        if (fi.Length < MaxFileSize) return;

        // Delete oldest backup
        var oldest = _logFilePath + $".{MaxBackups}";
        if (File.Exists(oldest)) File.Delete(oldest);

        // Shift backups: .2 → .3, .1 → .2
        for (int i = MaxBackups - 1; i >= 1; i--)
        {
            var src = _logFilePath + $".{i}";
            var dst = _logFilePath + $".{i + 1}";
            if (File.Exists(src)) File.Move(src, dst);
        }

        // Current → .1
        File.Move(_logFilePath, _logFilePath + ".1");
    }
}
