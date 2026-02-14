using Microsoft.AspNetCore.Hosting;

namespace SimplerJiangAiAgent.Api.Infrastructure.Logging;

public interface IFileLogWriter
{
    void Write(string category, string message);
}

public sealed class FileLogWriter : IFileLogWriter
{
    private readonly string _logFilePath;
    private readonly object _sync = new();

    public FileLogWriter(IWebHostEnvironment environment)
    {
        var logDir = Path.Combine(environment.ContentRootPath, "App_Data", "logs");
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
