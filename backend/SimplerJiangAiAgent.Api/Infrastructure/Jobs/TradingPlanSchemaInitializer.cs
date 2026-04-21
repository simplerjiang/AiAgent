using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public static class TradingPlanSchemaInitializer
{
    private static readonly Regex SafeIdentifier =
        new(@"^\w+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> AllowedColumnTypes =
        new(StringComparer.OrdinalIgnoreCase) { "TEXT", "REAL", "INTEGER", "BLOB", "NUMERIC" };

    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;
        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureSqliteAsync(dbContext, cancellationToken);
            return;
        }

        if (!provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('dbo.TradingPlans', 'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE dbo.TradingPlans(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TradingPlans PRIMARY KEY, " +
            "Symbol NVARCHAR(32) NOT NULL, " +
            "Name NVARCHAR(128) NOT NULL, " +
            "Direction NVARCHAR(16) NOT NULL, " +
            "Status NVARCHAR(16) NOT NULL, " +
            "TriggerPrice DECIMAL(18,2) NULL, " +
            "InvalidPrice DECIMAL(18,2) NULL, " +
            "StopLossPrice DECIMAL(18,2) NULL, " +
            "TakeProfitPrice DECIMAL(18,2) NULL, " +
            "TargetPrice DECIMAL(18,2) NULL, " +
            "ExpectedCatalyst NVARCHAR(MAX) NULL, " +
            "InvalidConditions NVARCHAR(MAX) NULL, " +
            "RiskLimits NVARCHAR(MAX) NULL, " +
            "AnalysisSummary NVARCHAR(MAX) NULL, " +
            "AnalysisHistoryId BIGINT NULL, " +
            "SourceAgent NVARCHAR(64) NOT NULL CONSTRAINT DF_TradingPlans_SourceAgent DEFAULT('commander'), " +
            "UserNote NVARCHAR(MAX) NULL, " +
            "ActiveScenario NVARCHAR(32) NULL, " +
            "PlanStartDate DATE NULL, " +
            "PlanEndDate DATE NULL, " +
            "MarketStageLabelAtCreation NVARCHAR(16) NULL, " +
            "StageConfidenceAtCreation DECIMAL(18,2) NULL, " +
            "SuggestedPositionScale DECIMAL(18,4) NULL, " +
            "ExecutionFrequencyLabel NVARCHAR(32) NULL, " +
            "MainlineSectorName NVARCHAR(128) NULL, " +
            "MainlineScoreAtCreation DECIMAL(18,2) NULL, " +
            "SectorNameAtCreation NVARCHAR(128) NULL, " +
            "SectorCodeAtCreation NVARCHAR(32) NULL, " +
            "CreatedAt DATETIME2 NOT NULL, " +
            "UpdatedAt DATETIME2 NOT NULL, " +
            "TriggeredAt DATETIME2 NULL, " +
            "InvalidatedAt DATETIME2 NULL, " +
            "CancelledAt DATETIME2 NULL" +
            "); " +
            "END; " +
            "IF COL_LENGTH('dbo.TradingPlans','PlanKey') IS NULL ALTER TABLE dbo.TradingPlans ADD PlanKey NVARCHAR(64) NOT NULL CONSTRAINT DF_TradingPlans_PlanKey DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradingPlans','Title') IS NULL ALTER TABLE dbo.TradingPlans ADD Title NVARCHAR(450) NOT NULL CONSTRAINT DF_TradingPlans_Title DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradingPlans','Symbol') IS NULL ALTER TABLE dbo.TradingPlans ADD Symbol NVARCHAR(32) NOT NULL CONSTRAINT DF_TradingPlans_Symbol DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradingPlans','Name') IS NULL ALTER TABLE dbo.TradingPlans ADD Name NVARCHAR(128) NOT NULL CONSTRAINT DF_TradingPlans_Name DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradingPlans','Direction') IS NULL ALTER TABLE dbo.TradingPlans ADD Direction NVARCHAR(16) NOT NULL CONSTRAINT DF_TradingPlans_Direction DEFAULT('Long'); " +
            "IF COL_LENGTH('dbo.TradingPlans','TriggerPrice') IS NULL ALTER TABLE dbo.TradingPlans ADD TriggerPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','InvalidPrice') IS NULL ALTER TABLE dbo.TradingPlans ADD InvalidPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','StopLossPrice') IS NULL ALTER TABLE dbo.TradingPlans ADD StopLossPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','TakeProfitPrice') IS NULL ALTER TABLE dbo.TradingPlans ADD TakeProfitPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','TargetPrice') IS NULL ALTER TABLE dbo.TradingPlans ADD TargetPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','ExpectedCatalyst') IS NULL ALTER TABLE dbo.TradingPlans ADD ExpectedCatalyst NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','InvalidConditions') IS NULL ALTER TABLE dbo.TradingPlans ADD InvalidConditions NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','RiskLimits') IS NULL ALTER TABLE dbo.TradingPlans ADD RiskLimits NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','AnalysisSummary') IS NULL ALTER TABLE dbo.TradingPlans ADD AnalysisSummary NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','AnalysisHistoryId') IS NULL ALTER TABLE dbo.TradingPlans ADD AnalysisHistoryId BIGINT NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','SourceAgent') IS NULL ALTER TABLE dbo.TradingPlans ADD SourceAgent NVARCHAR(64) NOT NULL CONSTRAINT DF_TradingPlans_SourceAgent DEFAULT('commander'); " +
            "IF COL_LENGTH('dbo.TradingPlans','UserNote') IS NULL ALTER TABLE dbo.TradingPlans ADD UserNote NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','ActiveScenario') IS NULL ALTER TABLE dbo.TradingPlans ADD ActiveScenario NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','PlanStartDate') IS NULL ALTER TABLE dbo.TradingPlans ADD PlanStartDate DATE NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','PlanEndDate') IS NULL ALTER TABLE dbo.TradingPlans ADD PlanEndDate DATE NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','MarketStageLabelAtCreation') IS NULL ALTER TABLE dbo.TradingPlans ADD MarketStageLabelAtCreation NVARCHAR(16) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','StageConfidenceAtCreation') IS NULL ALTER TABLE dbo.TradingPlans ADD StageConfidenceAtCreation DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','SuggestedPositionScale') IS NULL ALTER TABLE dbo.TradingPlans ADD SuggestedPositionScale DECIMAL(18,4) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','ExecutionFrequencyLabel') IS NULL ALTER TABLE dbo.TradingPlans ADD ExecutionFrequencyLabel NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','MainlineSectorName') IS NULL ALTER TABLE dbo.TradingPlans ADD MainlineSectorName NVARCHAR(128) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','MainlineScoreAtCreation') IS NULL ALTER TABLE dbo.TradingPlans ADD MainlineScoreAtCreation DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','SectorNameAtCreation') IS NULL ALTER TABLE dbo.TradingPlans ADD SectorNameAtCreation NVARCHAR(128) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','SectorCodeAtCreation') IS NULL ALTER TABLE dbo.TradingPlans ADD SectorCodeAtCreation NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','TriggeredAt') IS NULL ALTER TABLE dbo.TradingPlans ADD TriggeredAt DATETIME2 NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','InvalidatedAt') IS NULL ALTER TABLE dbo.TradingPlans ADD InvalidatedAt DATETIME2 NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','CancelledAt') IS NULL ALTER TABLE dbo.TradingPlans ADD CancelledAt DATETIME2 NULL; " +
            BuildSqlServerAnalysisHistoryCompatibilitySql() +
            "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TradingPlans') AND name = 'Status' AND max_length < 28) ALTER TABLE dbo.TradingPlans ALTER COLUMN Status NVARCHAR(16) NOT NULL; " +
            "IF COL_LENGTH('dbo.TradingPlans','PlanKey') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID('dbo.TradingPlans') AND c.name = 'PlanKey') ALTER TABLE dbo.TradingPlans ADD CONSTRAINT DF_TradingPlans_LegacyPlanKey DEFAULT('') FOR PlanKey; " +
            "IF COL_LENGTH('dbo.TradingPlans','Title') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID('dbo.TradingPlans') AND c.name = 'Title') ALTER TABLE dbo.TradingPlans ADD CONSTRAINT DF_TradingPlans_LegacyTitle DEFAULT('') FOR Title; " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlans_PlanKey' AND object_id = OBJECT_ID('dbo.TradingPlans')) CREATE UNIQUE INDEX IX_TradingPlans_PlanKey ON dbo.TradingPlans(PlanKey); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlans_Symbol_CreatedAt' AND object_id = OBJECT_ID('dbo.TradingPlans')) " +
            "CREATE INDEX IX_TradingPlans_Symbol_CreatedAt ON dbo.TradingPlans(Symbol, CreatedAt); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlans_AnalysisHistoryId' AND object_id = OBJECT_ID('dbo.TradingPlans')) " +
            "CREATE INDEX IX_TradingPlans_AnalysisHistoryId ON dbo.TradingPlans(AnalysisHistoryId); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlans_SectorCodeAtCreation' AND object_id = OBJECT_ID('dbo.TradingPlans')) " +
            "CREATE INDEX IX_TradingPlans_SectorCodeAtCreation ON dbo.TradingPlans(SectorCodeAtCreation); " +
            "IF OBJECT_ID('dbo.TradingPlanEvents', 'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE dbo.TradingPlanEvents(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TradingPlanEvents PRIMARY KEY, " +
            "PlanId BIGINT NOT NULL, " +
            "Symbol NVARCHAR(32) NOT NULL, " +
            "EventType NVARCHAR(32) NOT NULL, " +
            "Severity NVARCHAR(16) NOT NULL, " +
            "Message NVARCHAR(MAX) NOT NULL, " +
            "SnapshotPrice DECIMAL(18,2) NULL, " +
            "MetadataJson NVARCHAR(MAX) NULL, " +
            "OccurredAt DATETIME2 NOT NULL" +
            "); " +
            "END; " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','PlanId') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD PlanId BIGINT NOT NULL CONSTRAINT DF_TradingPlanEvents_PlanId DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','VersionId') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD VersionId BIGINT NULL; " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','Symbol') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Symbol NVARCHAR(32) NOT NULL CONSTRAINT DF_TradingPlanEvents_Symbol DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','EventType') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD EventType NVARCHAR(32) NOT NULL CONSTRAINT DF_TradingPlanEvents_EventType DEFAULT('VolumeDivergenceWarning'); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','Strategy') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Strategy NVARCHAR(64) NOT NULL CONSTRAINT DF_TradingPlanEvents_Strategy DEFAULT('runtime'); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','Reason') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Reason NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','CreatedAt') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TradingPlanEvents_CreatedAt DEFAULT(SYSUTCDATETIME()); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','Severity') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Severity NVARCHAR(16) NOT NULL CONSTRAINT DF_TradingPlanEvents_Severity DEFAULT('Warning'); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','Message') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Message NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TradingPlanEvents_Message DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','SnapshotPrice') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD SnapshotPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','MetadataJson') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD MetadataJson NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradingPlanEvents','OccurredAt') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD OccurredAt DATETIME2 NOT NULL CONSTRAINT DF_TradingPlanEvents_OccurredAt DEFAULT(SYSUTCDATETIME()); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlanEvents_PlanId_OccurredAt' AND object_id = OBJECT_ID('dbo.TradingPlanEvents')) CREATE INDEX IX_TradingPlanEvents_PlanId_OccurredAt ON dbo.TradingPlanEvents(PlanId, OccurredAt); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlanEvents_Symbol_OccurredAt' AND object_id = OBJECT_ID('dbo.TradingPlanEvents')) CREATE INDEX IX_TradingPlanEvents_Symbol_OccurredAt ON dbo.TradingPlanEvents(Symbol, OccurredAt);",
            cancellationToken);
    }

    internal static string BuildSqlServerAnalysisHistoryCompatibilitySql()
    {
        return
            "DECLARE @analysisHistoryDefaultConstraint SYSNAME; " +
            "SELECT @analysisHistoryDefaultConstraint = dc.name FROM sys.default_constraints dc JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id WHERE dc.parent_object_id = OBJECT_ID('dbo.TradingPlans') AND c.name = 'AnalysisHistoryId'; " +
            "IF @analysisHistoryDefaultConstraint IS NOT NULL EXEC('ALTER TABLE dbo.TradingPlans DROP CONSTRAINT [' + @analysisHistoryDefaultConstraint + ']'); " +
            "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TradingPlans') AND name = 'AnalysisHistoryId' AND is_nullable = 0) ALTER TABLE dbo.TradingPlans ALTER COLUMN AnalysisHistoryId BIGINT NULL; " +
            "IF EXISTS (SELECT 1 FROM dbo.TradingPlans WHERE AnalysisHistoryId <= 0) UPDATE dbo.TradingPlans SET AnalysisHistoryId = NULL WHERE AnalysisHistoryId <= 0; ";
    }

    private static async Task EnsureSqliteAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS TradingPlans (
                Id                          INTEGER PRIMARY KEY AUTOINCREMENT,
                PlanKey                     TEXT    NOT NULL DEFAULT '',
                Title                       TEXT    NOT NULL DEFAULT '',
                Symbol                      TEXT    NOT NULL DEFAULT '',
                Name                        TEXT    NOT NULL DEFAULT '',
                Direction                   TEXT    NOT NULL DEFAULT 'Long',
                Status                      TEXT    NOT NULL DEFAULT 'Pending',
                TriggerPrice                REAL    NULL,
                InvalidPrice                REAL    NULL,
                StopLossPrice               REAL    NULL,
                TakeProfitPrice             REAL    NULL,
                TargetPrice                 REAL    NULL,
                ExpectedCatalyst            TEXT    NULL,
                InvalidConditions           TEXT    NULL,
                RiskLimits                  TEXT    NULL,
                AnalysisSummary             TEXT    NULL,
                AnalysisHistoryId           INTEGER NULL,
                SourceAgent                 TEXT    NOT NULL DEFAULT 'commander',
                UserNote                    TEXT    NULL,
                ActiveScenario              TEXT    NULL,
                PlanStartDate               TEXT    NULL,
                PlanEndDate                 TEXT    NULL,
                MarketStageLabelAtCreation  TEXT    NULL,
                StageConfidenceAtCreation   REAL    NULL,
                SuggestedPositionScale      REAL    NULL,
                ExecutionFrequencyLabel     TEXT    NULL,
                MainlineSectorName          TEXT    NULL,
                MainlineScoreAtCreation     REAL    NULL,
                SectorNameAtCreation        TEXT    NULL,
                SectorCodeAtCreation        TEXT    NULL,
                CreatedAt                   TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00',
                UpdatedAt                   TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00',
                TriggeredAt                 TEXT    NULL,
                InvalidatedAt               TEXT    NULL,
                CancelledAt                 TEXT    NULL,
                FOREIGN KEY (AnalysisHistoryId) REFERENCES StockAgentAnalysisHistories (Id) ON DELETE RESTRICT
            );", cancellationToken);

        var columns = await GetSqliteTableColumnsAsync(dbContext, "TradingPlans", cancellationToken);
        var analysisHistoryIdColumn = columns.FirstOrDefault(column => string.Equals(column.Name, "AnalysisHistoryId", StringComparison.OrdinalIgnoreCase));
        if (analysisHistoryIdColumn is null || analysisHistoryIdColumn.NotNull || DefaultIsZero(analysisHistoryIdColumn.DefaultValue))
        {
            await RebuildSqliteTradingPlansAsync(dbContext, columns, cancellationToken);
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_TradingPlans_PlanKey ON TradingPlans(PlanKey);", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_TradingPlans_Symbol_CreatedAt ON TradingPlans(Symbol, CreatedAt);", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_TradingPlans_AnalysisHistoryId ON TradingPlans(AnalysisHistoryId);", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_TradingPlans_SectorCodeAtCreation ON TradingPlans(SectorCodeAtCreation);", cancellationToken);
    }

    private static async Task RebuildSqliteTradingPlansAsync(AppDbContext dbContext, IReadOnlyList<SqliteTableColumnInfo> columns, CancellationToken cancellationToken)
    {
        var existingColumns = columns
            .Select(column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF;", cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"TradingPlans\" RENAME TO \"TradingPlans__legacy_analysis_history_fix\";",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE TradingPlans (
                    Id                          INTEGER PRIMARY KEY AUTOINCREMENT,
                    PlanKey                     TEXT    NOT NULL DEFAULT '',
                    Title                       TEXT    NOT NULL DEFAULT '',
                    Symbol                      TEXT    NOT NULL DEFAULT '',
                    Name                        TEXT    NOT NULL DEFAULT '',
                    Direction                   TEXT    NOT NULL DEFAULT 'Long',
                    Status                      TEXT    NOT NULL DEFAULT 'Pending',
                    TriggerPrice                REAL    NULL,
                    InvalidPrice                REAL    NULL,
                    StopLossPrice               REAL    NULL,
                    TakeProfitPrice             REAL    NULL,
                    TargetPrice                 REAL    NULL,
                    ExpectedCatalyst            TEXT    NULL,
                    InvalidConditions           TEXT    NULL,
                    RiskLimits                  TEXT    NULL,
                    AnalysisSummary             TEXT    NULL,
                    AnalysisHistoryId           INTEGER NULL,
                    SourceAgent                 TEXT    NOT NULL DEFAULT 'commander',
                    UserNote                    TEXT    NULL,
                    ActiveScenario              TEXT    NULL,
                    PlanStartDate               TEXT    NULL,
                    PlanEndDate                 TEXT    NULL,
                    MarketStageLabelAtCreation  TEXT    NULL,
                    StageConfidenceAtCreation   REAL    NULL,
                    SuggestedPositionScale      REAL    NULL,
                    ExecutionFrequencyLabel     TEXT    NULL,
                    MainlineSectorName          TEXT    NULL,
                    MainlineScoreAtCreation     REAL    NULL,
                    SectorNameAtCreation        TEXT    NULL,
                    SectorCodeAtCreation        TEXT    NULL,
                    CreatedAt                   TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00',
                    UpdatedAt                   TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00',
                    TriggeredAt                 TEXT    NULL,
                    InvalidatedAt               TEXT    NULL,
                    CancelledAt                 TEXT    NULL,
                    FOREIGN KEY (AnalysisHistoryId) REFERENCES StockAgentAnalysisHistories (Id) ON DELETE RESTRICT
                );", cancellationToken);

            var insertColumns = string.Join(", ", SqliteTradingPlanColumns.Select(column => $"\"{column}\""));
            var selectColumns = string.Join(", ", SqliteTradingPlanColumns.Select(column => BuildSqliteTradingPlanCopyExpression(existingColumns, column)));
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"TradingPlans\" ({insertColumns}) SELECT {selectColumns} FROM \"TradingPlans__legacy_analysis_history_fix\";",
                cancellationToken);
#pragma warning restore EF1002

            await dbContext.Database.ExecuteSqlRawAsync(
                "DROP TABLE \"TradingPlans__legacy_analysis_history_fix\";",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", cancellationToken);
        }
    }

    private static readonly string[] SqliteTradingPlanColumns =
    {
        "Id",
        "PlanKey",
        "Title",
        "Symbol",
        "Name",
        "Direction",
        "Status",
        "TriggerPrice",
        "InvalidPrice",
        "StopLossPrice",
        "TakeProfitPrice",
        "TargetPrice",
        "ExpectedCatalyst",
        "InvalidConditions",
        "RiskLimits",
        "AnalysisSummary",
        "AnalysisHistoryId",
        "SourceAgent",
        "UserNote",
        "ActiveScenario",
        "PlanStartDate",
        "PlanEndDate",
        "MarketStageLabelAtCreation",
        "StageConfidenceAtCreation",
        "SuggestedPositionScale",
        "ExecutionFrequencyLabel",
        "MainlineSectorName",
        "MainlineScoreAtCreation",
        "SectorNameAtCreation",
        "SectorCodeAtCreation",
        "CreatedAt",
        "UpdatedAt",
        "TriggeredAt",
        "InvalidatedAt",
        "CancelledAt"
    };

    private static string BuildSqliteTradingPlanCopyExpression(HashSet<string> existingColumns, string column)
    {
        static string Column(string name) => $"\"{name}\"";

        var hasId = existingColumns.Contains("Id");
        var analysisHistoryProjection = existingColumns.Contains("AnalysisHistoryId")
            ? "CASE WHEN \"AnalysisHistoryId\" IS NULL OR \"AnalysisHistoryId\" <= 0 THEN NULL ELSE \"AnalysisHistoryId\" END"
            : "NULL";

        return column switch
        {
            "Id" => hasId ? Column("Id") : "NULL",
            "PlanKey" => existingColumns.Contains("PlanKey")
                ? $"COALESCE(NULLIF(TRIM({Column("PlanKey")}), ''), 'plan-legacy-' || {Column("Id")})"
                : hasId ? $"'plan-legacy-' || {Column("Id")}" : "lower(hex(randomblob(16)))",
            "Title" => existingColumns.Contains("Title")
                ? $"COALESCE(NULLIF(TRIM({Column("Title")}), ''), {BuildNullableTextProjection(existingColumns, "Name", "''")})"
                : BuildNullableTextProjection(existingColumns, "Name", "''"),
            "Symbol" => BuildNullableTextProjection(existingColumns, "Symbol", "''"),
            "Name" => BuildNullableTextProjection(existingColumns, "Name", "''"),
            "Direction" => existingColumns.Contains("Direction")
                ? $"COALESCE(NULLIF(TRIM({Column("Direction")}), ''), 'Long')"
                : "'Long'",
            "Status" => existingColumns.Contains("Status")
                ? $"COALESCE(NULLIF(TRIM({Column("Status")}), ''), 'Pending')"
                : "'Pending'",
            "TriggerPrice" => BuildNullableValueProjection(existingColumns, "TriggerPrice"),
            "InvalidPrice" => BuildNullableValueProjection(existingColumns, "InvalidPrice"),
            "StopLossPrice" => BuildNullableValueProjection(existingColumns, "StopLossPrice"),
            "TakeProfitPrice" => BuildNullableValueProjection(existingColumns, "TakeProfitPrice"),
            "TargetPrice" => BuildNullableValueProjection(existingColumns, "TargetPrice"),
            "ExpectedCatalyst" => BuildNullableValueProjection(existingColumns, "ExpectedCatalyst"),
            "InvalidConditions" => BuildNullableValueProjection(existingColumns, "InvalidConditions"),
            "RiskLimits" => BuildNullableValueProjection(existingColumns, "RiskLimits"),
            "AnalysisSummary" => BuildNullableValueProjection(existingColumns, "AnalysisSummary"),
            "AnalysisHistoryId" => analysisHistoryProjection,
            "SourceAgent" => existingColumns.Contains("SourceAgent")
                ? $"CASE WHEN {Column("SourceAgent")} IS NULL OR TRIM({Column("SourceAgent")}) = '' THEN CASE WHEN {analysisHistoryProjection} IS NULL THEN 'manual' ELSE 'commander' END ELSE {Column("SourceAgent")} END"
                : $"CASE WHEN {analysisHistoryProjection} IS NULL THEN 'manual' ELSE 'commander' END",
            "UserNote" => BuildNullableValueProjection(existingColumns, "UserNote"),
            "ActiveScenario" => BuildNullableValueProjection(existingColumns, "ActiveScenario"),
            "PlanStartDate" => BuildNullableValueProjection(existingColumns, "PlanStartDate"),
            "PlanEndDate" => BuildNullableValueProjection(existingColumns, "PlanEndDate"),
            "MarketStageLabelAtCreation" => BuildNullableValueProjection(existingColumns, "MarketStageLabelAtCreation"),
            "StageConfidenceAtCreation" => BuildNullableValueProjection(existingColumns, "StageConfidenceAtCreation"),
            "SuggestedPositionScale" => BuildNullableValueProjection(existingColumns, "SuggestedPositionScale"),
            "ExecutionFrequencyLabel" => BuildNullableValueProjection(existingColumns, "ExecutionFrequencyLabel"),
            "MainlineSectorName" => BuildNullableValueProjection(existingColumns, "MainlineSectorName"),
            "MainlineScoreAtCreation" => BuildNullableValueProjection(existingColumns, "MainlineScoreAtCreation"),
            "SectorNameAtCreation" => BuildNullableValueProjection(existingColumns, "SectorNameAtCreation"),
            "SectorCodeAtCreation" => BuildNullableValueProjection(existingColumns, "SectorCodeAtCreation"),
            "CreatedAt" => existingColumns.Contains("CreatedAt") ? Column("CreatedAt") : "CURRENT_TIMESTAMP",
            "UpdatedAt" => existingColumns.Contains("UpdatedAt") ? Column("UpdatedAt") : "CURRENT_TIMESTAMP",
            "TriggeredAt" => BuildNullableValueProjection(existingColumns, "TriggeredAt"),
            "InvalidatedAt" => BuildNullableValueProjection(existingColumns, "InvalidatedAt"),
            "CancelledAt" => BuildNullableValueProjection(existingColumns, "CancelledAt"),
            _ => throw new InvalidOperationException($"Unexpected TradingPlans column '{column}'.")
        };
    }

    private static string BuildNullableTextProjection(HashSet<string> existingColumns, string column, string fallbackExpression)
    {
        return existingColumns.Contains(column)
            ? $"COALESCE(NULLIF(TRIM(\"{column}\"), ''), {fallbackExpression})"
            : fallbackExpression;
    }

    private static string BuildNullableValueProjection(HashSet<string> existingColumns, string column)
    {
        return existingColumns.Contains(column)
            ? $"\"{column}\""
            : "NULL";
    }

    private static bool DefaultIsZero(string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(defaultValue))
        {
            return false;
        }

        var normalized = defaultValue.Trim().Trim('(', ')', '\'', '"');
        return normalized == "0";
    }

    private static async Task<IReadOnlyList<SqliteTableColumnInfo>> GetSqliteTableColumnsAsync(AppDbContext dbContext, string tableName, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            var result = new List<SqliteTableColumnInfo>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new SqliteTableColumnInfo(
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetInt32(3) != 0,
                    reader.IsDBNull(4) ? null : reader.GetValue(4)?.ToString()));
            }

            return result;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureSqliteColumnAsync(AppDbContext dbContext, string table, string column, string sqlType, string? defaultValue, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(table) || !SafeIdentifier.IsMatch(table))
            throw new ArgumentException($"Unsafe table name: '{table}'.", nameof(table));
        if (string.IsNullOrWhiteSpace(column) || !SafeIdentifier.IsMatch(column))
            throw new ArgumentException($"Unsafe column name: '{column}'.", nameof(column));
        if (!AllowedColumnTypes.Contains(sqlType))
            throw new ArgumentException($"Disallowed column type: '{sqlType}'.", nameof(sqlType));

        var defaultClause = defaultValue is not null ? $" NOT NULL DEFAULT {defaultValue}" : " NULL";
        try
        {
#pragma warning disable EF1002
            await dbContext.Database.ExecuteSqlRawAsync(
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {sqlType}{defaultClause};", ct);
#pragma warning restore EF1002
        }
        catch
        {
            // Column already exists — safe to ignore.
        }
    }

    private sealed record SqliteTableColumnInfo(string Name, string? Type, bool NotNull, string? DefaultValue);
}