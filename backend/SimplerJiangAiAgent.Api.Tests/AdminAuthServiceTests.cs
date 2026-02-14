using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SimplerJiangAiAgent.Api.Infrastructure.Security;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class AdminAuthServiceTests
{
    [Fact]
    public void IssueToken_ShouldBeValid()
    {
        var options = Options.Create(new AdminOptions
        {
            Username = "admin",
            Password = "pwd",
            TokenExpiryMinutes = 10
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new AdminAuthService(cache, options);

        Assert.True(service.ValidateCredentials("admin", "pwd"));

        var token = service.IssueToken();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(service.IsTokenValid(token));
        Assert.True(service.GetExpiry(token) > DateTimeOffset.UtcNow);
    }
}
