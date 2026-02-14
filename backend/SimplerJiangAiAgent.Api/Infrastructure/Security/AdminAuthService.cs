using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SimplerJiangAiAgent.Api.Infrastructure.Security;

public interface IAdminAuthService
{
    bool ValidateCredentials(string username, string password);
    string IssueToken();
    bool IsTokenValid(string token);
    DateTimeOffset GetExpiry(string token);
}

public sealed class AdminAuthService : IAdminAuthService
{
    private readonly IMemoryCache _cache;
    private readonly AdminOptions _options;

    public AdminAuthService(IMemoryCache cache, IOptions<AdminOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public bool ValidateCredentials(string username, string password)
    {
        return string.Equals(username, _options.Username, StringComparison.Ordinal)
            && string.Equals(password, _options.Password, StringComparison.Ordinal);
    }

    public string IssueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(Math.Max(5, _options.TokenExpiryMinutes));
        _cache.Set(token, expiry, expiry);
        return token;
    }

    public bool IsTokenValid(string token)
    {
        return _cache.TryGetValue(token, out DateTimeOffset _);
    }

    public DateTimeOffset GetExpiry(string token)
    {
        return _cache.TryGetValue(token, out DateTimeOffset expiry)
            ? expiry
            : DateTimeOffset.UtcNow;
    }
}
