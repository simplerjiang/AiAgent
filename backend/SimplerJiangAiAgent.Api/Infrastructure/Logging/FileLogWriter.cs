using Microsoft.AspNetCore.Hosting;
using SimplerJiangAiAgent.Api.Infrastructure.Storage;

namespace SimplerJiangAiAgent.Api.Infrastructure.Logging;

public interface IFileLogWriter
{
    void Write(string category, string message);
}

public sealed class FileLogWriter : IFileLogWriter
{
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
            File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }
    }
}
