namespace SimplerJiangAiAgent.Api.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    // Provider: Sqlite | SqlServer | MySql
    public string Provider { get; set; } = "Sqlite";

    public string ConnectionString { get; set; } = string.Empty;

    public string DataRootPath { get; set; } = string.Empty;
}
