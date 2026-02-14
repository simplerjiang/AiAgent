namespace SimplerJiangAiAgent.Api.Infrastructure.Security;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "admin123";

    public int TokenExpiryMinutes { get; set; } = 120;
}
