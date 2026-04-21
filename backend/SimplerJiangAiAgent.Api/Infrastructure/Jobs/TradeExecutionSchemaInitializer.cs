using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public static class TradeExecutionSchemaInitializer
{
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
            // ── TradeExecutions table ──
            "IF OBJECT_ID('dbo.TradeExecutions', 'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE dbo.TradeExecutions(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TradeExecutions PRIMARY KEY, " +
            "PlanId BIGINT NULL, " +
            "Symbol NVARCHAR(32) NOT NULL, " +
            "Name NVARCHAR(128) NOT NULL, " +
            "Direction NVARCHAR(16) NOT NULL, " +
            "TradeType NVARCHAR(16) NOT NULL, " +
            "ExecutedPrice DECIMAL(18,2) NOT NULL, " +
            "Quantity INT NOT NULL, " +
            "ExecutedAt DATETIME2 NOT NULL, " +
            "Commission DECIMAL(18,2) NULL, " +
            "UserNote NVARCHAR(MAX) NULL, " +
            "CreatedAt DATETIME2 NOT NULL, " +
            "CostBasis DECIMAL(18,2) NULL, " +
            "RealizedPnL DECIMAL(18,2) NULL, " +
            "ReturnRate DECIMAL(18,6) NULL, " +
            "ComplianceTag NVARCHAR(32) NOT NULL, " +
            "AnalysisHistoryId BIGINT NULL, " +
            "AgentDirection NVARCHAR(16) NULL, " +
            "AgentConfidence DECIMAL(18,6) NULL, " +
            "MarketStageAtTrade NVARCHAR(16) NULL, " +
            "PlanSourceAgent NVARCHAR(64) NULL, " +
            "PlanAction NVARCHAR(32) NULL, " +
            "ExecutionAction NVARCHAR(32) NULL, " +
            "DeviationTagsJson NVARCHAR(MAX) NULL, " +
            "DeviationNote NVARCHAR(MAX) NULL, " +
            "AbandonReason NVARCHAR(MAX) NULL, " +
            "ScenarioCode NVARCHAR(32) NULL, " +
            "ScenarioLabel NVARCHAR(32) NULL, " +
            "ScenarioReason NVARCHAR(MAX) NULL, " +
            "ScenarioSnapshotType NVARCHAR(16) NULL, " +
            "ScenarioSnapshotJson NVARCHAR(MAX) NULL, " +
            "PositionSnapshotJson NVARCHAR(MAX) NULL, " +
            "CoachTip NVARCHAR(MAX) NULL" +
            "); " +
            "END; " +
            // Idempotent column adds
            "IF COL_LENGTH('dbo.TradeExecutions','PlanId') IS NULL ALTER TABLE dbo.TradeExecutions ADD PlanId BIGINT NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','Symbol') IS NULL ALTER TABLE dbo.TradeExecutions ADD Symbol NVARCHAR(32) NOT NULL CONSTRAINT DF_TradeExecutions_Symbol DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradeExecutions','Name') IS NULL ALTER TABLE dbo.TradeExecutions ADD Name NVARCHAR(128) NOT NULL CONSTRAINT DF_TradeExecutions_Name DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradeExecutions','Direction') IS NULL ALTER TABLE dbo.TradeExecutions ADD Direction NVARCHAR(16) NOT NULL CONSTRAINT DF_TradeExecutions_Direction DEFAULT('Buy'); " +
            "IF COL_LENGTH('dbo.TradeExecutions','TradeType') IS NULL ALTER TABLE dbo.TradeExecutions ADD TradeType NVARCHAR(16) NOT NULL CONSTRAINT DF_TradeExecutions_TradeType DEFAULT('Normal'); " +
            "IF COL_LENGTH('dbo.TradeExecutions','ExecutedPrice') IS NULL ALTER TABLE dbo.TradeExecutions ADD ExecutedPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_TradeExecutions_ExecutedPrice DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradeExecutions','Quantity') IS NULL ALTER TABLE dbo.TradeExecutions ADD Quantity INT NOT NULL CONSTRAINT DF_TradeExecutions_Quantity DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradeExecutions','ExecutedAt') IS NULL ALTER TABLE dbo.TradeExecutions ADD ExecutedAt DATETIME2 NOT NULL CONSTRAINT DF_TradeExecutions_ExecutedAt DEFAULT(SYSUTCDATETIME()); " +
            "IF COL_LENGTH('dbo.TradeExecutions','Commission') IS NULL ALTER TABLE dbo.TradeExecutions ADD Commission DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','UserNote') IS NULL ALTER TABLE dbo.TradeExecutions ADD UserNote NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','CreatedAt') IS NULL ALTER TABLE dbo.TradeExecutions ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TradeExecutions_CreatedAt DEFAULT(SYSUTCDATETIME()); " +
            "IF COL_LENGTH('dbo.TradeExecutions','CostBasis') IS NULL ALTER TABLE dbo.TradeExecutions ADD CostBasis DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','RealizedPnL') IS NULL ALTER TABLE dbo.TradeExecutions ADD RealizedPnL DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ReturnRate') IS NULL ALTER TABLE dbo.TradeExecutions ADD ReturnRate DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ReturnRate') IS NOT NULL ALTER TABLE dbo.TradeExecutions ALTER COLUMN ReturnRate DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ComplianceTag') IS NULL ALTER TABLE dbo.TradeExecutions ADD ComplianceTag NVARCHAR(32) NOT NULL CONSTRAINT DF_TradeExecutions_ComplianceTag DEFAULT('Unplanned'); " +
            "IF COL_LENGTH('dbo.TradeExecutions','AnalysisHistoryId') IS NULL ALTER TABLE dbo.TradeExecutions ADD AnalysisHistoryId BIGINT NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','AgentDirection') IS NULL ALTER TABLE dbo.TradeExecutions ADD AgentDirection NVARCHAR(16) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','AgentConfidence') IS NULL ALTER TABLE dbo.TradeExecutions ADD AgentConfidence DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','AgentConfidence') IS NOT NULL ALTER TABLE dbo.TradeExecutions ALTER COLUMN AgentConfidence DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','MarketStageAtTrade') IS NULL ALTER TABLE dbo.TradeExecutions ADD MarketStageAtTrade NVARCHAR(16) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','PlanSourceAgent') IS NULL ALTER TABLE dbo.TradeExecutions ADD PlanSourceAgent NVARCHAR(64) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','PlanAction') IS NULL ALTER TABLE dbo.TradeExecutions ADD PlanAction NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ExecutionAction') IS NULL ALTER TABLE dbo.TradeExecutions ADD ExecutionAction NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','DeviationTagsJson') IS NULL ALTER TABLE dbo.TradeExecutions ADD DeviationTagsJson NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','DeviationNote') IS NULL ALTER TABLE dbo.TradeExecutions ADD DeviationNote NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','AbandonReason') IS NULL ALTER TABLE dbo.TradeExecutions ADD AbandonReason NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ScenarioCode') IS NULL ALTER TABLE dbo.TradeExecutions ADD ScenarioCode NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ScenarioLabel') IS NULL ALTER TABLE dbo.TradeExecutions ADD ScenarioLabel NVARCHAR(32) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ScenarioReason') IS NULL ALTER TABLE dbo.TradeExecutions ADD ScenarioReason NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ScenarioSnapshotType') IS NULL ALTER TABLE dbo.TradeExecutions ADD ScenarioSnapshotType NVARCHAR(16) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','ScenarioSnapshotJson') IS NULL ALTER TABLE dbo.TradeExecutions ADD ScenarioSnapshotJson NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','PositionSnapshotJson') IS NULL ALTER TABLE dbo.TradeExecutions ADD PositionSnapshotJson NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeExecutions','CoachTip') IS NULL ALTER TABLE dbo.TradeExecutions ADD CoachTip NVARCHAR(MAX) NULL; " +
            // Indexes
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeExecutions_Symbol_ExecutedAt' AND object_id = OBJECT_ID('dbo.TradeExecutions')) " +
            "CREATE INDEX IX_TradeExecutions_Symbol_ExecutedAt ON dbo.TradeExecutions(Symbol, ExecutedAt); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeExecutions_PlanId' AND object_id = OBJECT_ID('dbo.TradeExecutions')) " +
            "CREATE INDEX IX_TradeExecutions_PlanId ON dbo.TradeExecutions(PlanId); " +

            // ── UserPortfolioSettings table ──
            "IF OBJECT_ID('dbo.UserPortfolioSettings', 'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE dbo.UserPortfolioSettings(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPortfolioSettings PRIMARY KEY, " +
            "TotalCapital DECIMAL(18,2) NOT NULL, " +
            "UpdatedAt DATETIME2 NOT NULL" +
            "); " +
            "END; " +
            "IF COL_LENGTH('dbo.UserPortfolioSettings','TotalCapital') IS NULL ALTER TABLE dbo.UserPortfolioSettings ADD TotalCapital DECIMAL(18,2) NOT NULL CONSTRAINT DF_UserPortfolioSettings_TotalCapital DEFAULT(0); " +
            "IF COL_LENGTH('dbo.UserPortfolioSettings','UpdatedAt') IS NULL ALTER TABLE dbo.UserPortfolioSettings ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserPortfolioSettings_UpdatedAt DEFAULT(SYSUTCDATETIME()); " +

            // ── Extend StockPositions table ──
            "IF COL_LENGTH('dbo.StockPositions','Name') IS NULL ALTER TABLE dbo.StockPositions ADD Name NVARCHAR(128) NOT NULL CONSTRAINT DF_StockPositions_Name DEFAULT(''); " +
            "IF COL_LENGTH('dbo.StockPositions','TotalCost') IS NULL ALTER TABLE dbo.StockPositions ADD TotalCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_StockPositions_TotalCost DEFAULT(0); " +
            "IF COL_LENGTH('dbo.StockPositions','LatestPrice') IS NULL ALTER TABLE dbo.StockPositions ADD LatestPrice DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.StockPositions','MarketValue') IS NULL ALTER TABLE dbo.StockPositions ADD MarketValue DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.StockPositions','UnrealizedPnL') IS NULL ALTER TABLE dbo.StockPositions ADD UnrealizedPnL DECIMAL(18,2) NULL; " +
            "IF COL_LENGTH('dbo.StockPositions','UnrealizedReturnRate') IS NULL ALTER TABLE dbo.StockPositions ADD UnrealizedReturnRate DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.StockPositions','UnrealizedReturnRate') IS NOT NULL ALTER TABLE dbo.StockPositions ALTER COLUMN UnrealizedReturnRate DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.StockPositions','PositionRatio') IS NULL ALTER TABLE dbo.StockPositions ADD PositionRatio DECIMAL(18,6) NULL; " +
            "IF COL_LENGTH('dbo.StockPositions','PositionRatio') IS NOT NULL ALTER TABLE dbo.StockPositions ALTER COLUMN PositionRatio DECIMAL(18,6) NULL; " +

            // ── TradeReviews table ──
            "IF OBJECT_ID('dbo.TradeReviews', 'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE dbo.TradeReviews(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TradeReviews PRIMARY KEY, " +
            "ReviewType NVARCHAR(16) NOT NULL, " +
            "PeriodStart DATETIME2 NOT NULL, " +
            "PeriodEnd DATETIME2 NOT NULL, " +
            "TradeCount INT NOT NULL, " +
            "TotalPnL DECIMAL(18,2) NOT NULL, " +
            "WinRate DECIMAL(18,6) NOT NULL, " +
            "ComplianceRate DECIMAL(18,6) NOT NULL, " +
            "ReviewContent NVARCHAR(MAX) NOT NULL, " +
            "ContextSummaryJson NVARCHAR(MAX) NULL, " +
            "LlmTraceId NVARCHAR(64) NULL, " +
            "CreatedAt DATETIME2 NOT NULL" +
            "); " +
            "END; " +
            "IF COL_LENGTH('dbo.TradeReviews','ReviewType') IS NULL ALTER TABLE dbo.TradeReviews ADD ReviewType NVARCHAR(16) NOT NULL CONSTRAINT DF_TradeReviews_ReviewType DEFAULT('Custom'); " +
            "IF COL_LENGTH('dbo.TradeReviews','PeriodStart') IS NULL ALTER TABLE dbo.TradeReviews ADD PeriodStart DATETIME2 NOT NULL CONSTRAINT DF_TradeReviews_PeriodStart DEFAULT(SYSUTCDATETIME()); " +
            "IF COL_LENGTH('dbo.TradeReviews','PeriodEnd') IS NULL ALTER TABLE dbo.TradeReviews ADD PeriodEnd DATETIME2 NOT NULL CONSTRAINT DF_TradeReviews_PeriodEnd DEFAULT(SYSUTCDATETIME()); " +
            "IF COL_LENGTH('dbo.TradeReviews','TradeCount') IS NULL ALTER TABLE dbo.TradeReviews ADD TradeCount INT NOT NULL CONSTRAINT DF_TradeReviews_TradeCount DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradeReviews','TotalPnL') IS NULL ALTER TABLE dbo.TradeReviews ADD TotalPnL DECIMAL(18,2) NOT NULL CONSTRAINT DF_TradeReviews_TotalPnL DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradeReviews','WinRate') IS NULL ALTER TABLE dbo.TradeReviews ADD WinRate DECIMAL(18,6) NOT NULL CONSTRAINT DF_TradeReviews_WinRate DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradeReviews','ComplianceRate') IS NULL ALTER TABLE dbo.TradeReviews ADD ComplianceRate DECIMAL(18,6) NOT NULL CONSTRAINT DF_TradeReviews_ComplianceRate DEFAULT(0); " +
            "IF COL_LENGTH('dbo.TradeReviews','ReviewContent') IS NULL ALTER TABLE dbo.TradeReviews ADD ReviewContent NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TradeReviews_ReviewContent DEFAULT(''); " +
            "IF COL_LENGTH('dbo.TradeReviews','ContextSummaryJson') IS NULL ALTER TABLE dbo.TradeReviews ADD ContextSummaryJson NVARCHAR(MAX) NULL; " +
            "IF COL_LENGTH('dbo.TradeReviews','LlmTraceId') IS NULL ALTER TABLE dbo.TradeReviews ADD LlmTraceId NVARCHAR(64) NULL; " +
            "IF COL_LENGTH('dbo.TradeReviews','CreatedAt') IS NULL ALTER TABLE dbo.TradeReviews ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_TradeReviews_CreatedAt DEFAULT(SYSUTCDATETIME()); " +
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeReviews_ReviewType_PeriodStart' AND object_id = OBJECT_ID('dbo.TradeReviews')) " +
            "CREATE INDEX IX_TradeReviews_ReviewType_PeriodStart ON dbo.TradeReviews(ReviewType, PeriodStart);",
            cancellationToken);
    }

    private static async Task EnsureSqliteAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // ── TradeExecutions table ──
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS TradeExecutions (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                PlanId              INTEGER NULL,
                Symbol              TEXT    NOT NULL DEFAULT '',
                Name                TEXT    NOT NULL DEFAULT '',
                Direction           TEXT    NOT NULL DEFAULT 'Buy',
                TradeType           TEXT    NOT NULL DEFAULT 'Normal',
                ExecutedPrice       REAL    NOT NULL DEFAULT 0,
                Quantity            INTEGER NOT NULL DEFAULT 0,
                ExecutedAt          TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00',
                Commission          REAL    NULL,
                UserNote            TEXT    NULL,
                CreatedAt           TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00',
                CostBasis           REAL    NULL,
                RealizedPnL         REAL    NULL,
                ReturnRate          REAL    NULL,
                ComplianceTag       TEXT    NOT NULL DEFAULT 'Unknown',
                AnalysisHistoryId   INTEGER NULL,
                AgentDirection      TEXT    NULL,
                AgentConfidence     REAL    NULL,
                MarketStageAtTrade  TEXT    NULL,
                PlanSourceAgent     TEXT    NULL,
                PlanAction          TEXT    NULL,
                ExecutionAction     TEXT    NULL,
                DeviationTagsJson   TEXT    NULL,
                DeviationNote       TEXT    NULL,
                AbandonReason       TEXT    NULL,
                ScenarioCode        TEXT    NULL,
                ScenarioLabel       TEXT    NULL,
                ScenarioReason      TEXT    NULL,
                ScenarioSnapshotType TEXT   NULL,
                ScenarioSnapshotJson TEXT   NULL,
                PositionSnapshotJson TEXT   NULL,
                CoachTip            TEXT    NULL
            );", cancellationToken);

            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "PlanSourceAgent", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "PlanAction", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "ExecutionAction", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "DeviationTagsJson", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "DeviationNote", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "AbandonReason", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "ScenarioCode", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "ScenarioLabel", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "ScenarioReason", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "ScenarioSnapshotType", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "ScenarioSnapshotJson", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "PositionSnapshotJson", "TEXT", null, cancellationToken);
            await EnsureSqliteColumnAsync(dbContext, "TradeExecutions", "CoachTip", "TEXT", null, cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_TradeExecutions_Symbol_ExecutedAt ON TradeExecutions(Symbol, ExecutedAt);", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_TradeExecutions_PlanId ON TradeExecutions(PlanId);", cancellationToken);

        // ── UserPortfolioSettings table ──
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS UserPortfolioSettings (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                TotalCapital    REAL    NOT NULL DEFAULT 0,
                UpdatedAt       TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00'
            );", cancellationToken);

        // ── Extend StockPositions (columns added by GOAL-018) ──
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "Name", "TEXT", "''", cancellationToken);
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "TotalCost", "REAL", "0", cancellationToken);
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "LatestPrice", "REAL", null, cancellationToken);
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "MarketValue", "REAL", null, cancellationToken);
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "UnrealizedPnL", "REAL", null, cancellationToken);
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "UnrealizedReturnRate", "REAL", null, cancellationToken);
        await EnsureSqliteColumnAsync(dbContext, "StockPositions", "PositionRatio", "REAL", null, cancellationToken);

        // ── TradeReviews table ──
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS TradeReviews (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                ReviewType          TEXT    NOT NULL DEFAULT 'Weekly',
                PeriodStart         TEXT    NOT NULL,
                PeriodEnd           TEXT    NOT NULL,
                TradeCount          INTEGER NOT NULL DEFAULT 0,
                TotalPnL            REAL    NOT NULL DEFAULT 0,
                WinRate             REAL    NOT NULL DEFAULT 0,
                ComplianceRate      REAL    NOT NULL DEFAULT 0,
                ReviewContent       TEXT    NULL,
                ContextSummaryJson  TEXT    NULL,
                LlmTraceId         TEXT    NULL,
                CreatedAt           TEXT    NOT NULL DEFAULT '0001-01-01T00:00:00'
            );", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_TradeReviews_ReviewType_PeriodStart ON TradeReviews(ReviewType, PeriodStart);", cancellationToken);
    }

    private static readonly Regex SafeIdentifier =
        new(@"^\w+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> AllowedColumnTypes =
        new(StringComparer.OrdinalIgnoreCase) { "TEXT", "REAL", "INTEGER", "BLOB", "NUMERIC" };

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
#pragma warning disable EF1002 // DDL: identifiers are developer-controlled and validated above
            await dbContext.Database.ExecuteSqlRawAsync(
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {sqlType}{defaultClause};", ct);
#pragma warning restore EF1002
        }
        catch
        {
            // Column already exists — safe to ignore
        }
    }
}
