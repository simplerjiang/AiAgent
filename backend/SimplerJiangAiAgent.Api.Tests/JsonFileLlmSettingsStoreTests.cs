using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SimplerJiangAiAgent.Api.Infrastructure.Llm;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class JsonFileLlmSettingsStoreTests
{
    [Fact]
    public async Task GetProviderAsync_ShouldPreferLocalSecretFile()
    {
        var rootPath = CreateTempRoot();
        var appDataPath = Path.Combine(rootPath, "App_Data");
        Directory.CreateDirectory(appDataPath);

        await File.WriteAllTextAsync(
            Path.Combine(appDataPath, "llm-settings.json"),
            """
            {
              "providers": {
                "openai": {
                  "provider": "openai",
                  "apiKey": "tracked-key",
                  "baseUrl": "https://api.bltcy.ai",
                  "model": "gemini-test"
                }
              }
            }
            """);

        await File.WriteAllTextAsync(
            Path.Combine(appDataPath, "llm-settings.local.json"),
            """
            {
              "providers": {
                "openai": {
                  "provider": "openai",
                  "apiKey": "local-key"
                }
              }
            }
            """);

        var store = new JsonFileLlmSettingsStore(new FakeWebHostEnvironment(rootPath));

        var settings = await store.GetProviderAsync("openai");

        Assert.NotNull(settings);
        Assert.Equal("local-key", settings!.ApiKey);
        Assert.Equal("https://api.bltcy.ai", settings.BaseUrl);
        Assert.Equal("gemini-test", settings.Model);
    }

    [Fact]
    public async Task UpsertAsync_ShouldWriteApiKeyToIgnoredLocalFileOnly()
    {
        var rootPath = CreateTempRoot();
        var store = new JsonFileLlmSettingsStore(new FakeWebHostEnvironment(rootPath));

        var result = await store.UpsertAsync(new LlmProviderSettings
        {
            Provider = "openai",
            ApiKey = "local-secret-key",
            BaseUrl = "https://api.bltcy.ai",
            Model = "gemini-test",
            Enabled = true
        });

        var defaultsJson = await File.ReadAllTextAsync(Path.Combine(rootPath, "App_Data", "llm-settings.json"));
        var localJson = await File.ReadAllTextAsync(Path.Combine(rootPath, "App_Data", "llm-settings.local.json"));
        var defaultsDocument = JsonSerializer.Deserialize<LlmSettingsDocument>(defaultsJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal("local-secret-key", result.ApiKey);
        Assert.DoesNotContain("local-secret-key", defaultsJson, StringComparison.Ordinal);
        Assert.Contains("local-secret-key", localJson, StringComparison.Ordinal);
        Assert.NotNull(defaultsDocument);
        Assert.True(defaultsDocument!.Providers.TryGetValue("openai", out var defaultsSettings));
        Assert.Equal(string.Empty, defaultsSettings!.ApiKey);
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "SimplerJiangAiAgent.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
          ContentRootFileProvider = new NullFileProvider();
          WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; } = "SimplerJiangAiAgent.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}