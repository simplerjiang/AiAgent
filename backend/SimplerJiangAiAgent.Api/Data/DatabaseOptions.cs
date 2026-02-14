namespace SimplerJiangAiAgent.Api.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    // Provider: SqlServer | MySql
    public string Provider { get; set; } = "SqlServer";

    public string ConnectionString { get; set; } = string.Empty;
}
