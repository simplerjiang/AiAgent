using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;

namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services;

public static class ResearchSessionSchemaInitializer
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;
        if (!provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            return; // SQLite — EnsureCreated handles it

        await EnsureSqlServerAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureSqlServerAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchSessions', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchSessions(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchSessions PRIMARY KEY, " +
            "SessionKey NVARCHAR(128) NOT NULL, " +
            "Symbol NVARCHAR(32) NOT NULL, " +
            "Name NVARCHAR(256) NOT NULL, " +
            "Status NVARCHAR(32) NOT NULL, " +
            "ActiveTurnId BIGINT NULL, " +
            "ActiveStage NVARCHAR(64) NULL, " +
            "LastUserIntent NVARCHAR(MAX) NULL, " +
            "DegradedFlagsJson NVARCHAR(MAX) NULL, " +
            "LatestRating NVARCHAR(32) NULL, " +
            "LatestDecisionHeadline NVARCHAR(512) NULL, " +
            "CreatedAt DATETIME2 NOT NULL, " +
            "UpdatedAt DATETIME2 NOT NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchTurns', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchTurns(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchTurns PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnIndex INT NOT NULL, " +
            "UserPrompt NVARCHAR(MAX) NOT NULL, " +
            "Status NVARCHAR(32) NOT NULL, " +
            "ContinuationMode NVARCHAR(32) NOT NULL, " +
            "ReuseScope NVARCHAR(MAX) NULL, " +
            "RerunScope NVARCHAR(MAX) NULL, " +
            "ChangeSummary NVARCHAR(MAX) NULL, " +
            "StopReason NVARCHAR(MAX) NULL, " +
            "DegradedFlagsJson NVARCHAR(MAX) NULL, " +
            "RequestedAt DATETIME2 NOT NULL, " +
            "StartedAt DATETIME2 NULL, " +
            "CompletedAt DATETIME2 NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchStageSnapshots', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchStageSnapshots(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchStageSnapshots PRIMARY KEY, " +
            "TurnId BIGINT NOT NULL, " +
            "StageType NVARCHAR(64) NOT NULL, " +
            "StageRunIndex INT NOT NULL, " +
            "ExecutionMode NVARCHAR(32) NOT NULL, " +
            "Status NVARCHAR(32) NOT NULL, " +
            "ActiveRoleIdsJson NVARCHAR(MAX) NULL, " +
            "Summary NVARCHAR(MAX) NULL, " +
            "DegradedFlagsJson NVARCHAR(MAX) NULL, " +
            "StopReason NVARCHAR(MAX) NULL, " +
            "StartedAt DATETIME2 NULL, " +
            "CompletedAt DATETIME2 NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchRoleStates', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchRoleStates(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchRoleStates PRIMARY KEY, " +
            "StageId BIGINT NOT NULL, " +
            "RoleId NVARCHAR(64) NOT NULL, " +
            "RunIndex INT NOT NULL, " +
            "Status NVARCHAR(32) NOT NULL, " +
            "ToolPolicyClass NVARCHAR(64) NULL, " +
            "InputRefsJson NVARCHAR(MAX) NULL, " +
            "OutputRefsJson NVARCHAR(MAX) NULL, " +
            "OutputContentJson NVARCHAR(MAX) NULL, " +
            "DegradedFlagsJson NVARCHAR(MAX) NULL, " +
            "ErrorCode NVARCHAR(64) NULL, " +
            "ErrorMessage NVARCHAR(MAX) NULL, " +
            "LlmTraceId NVARCHAR(128) NULL, " +
            "StartedAt DATETIME2 NULL, " +
            "CompletedAt DATETIME2 NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchFeedItems', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchFeedItems(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchFeedItems PRIMARY KEY, " +
            "TurnId BIGINT NOT NULL, " +
            "StageId BIGINT NULL, " +
            "RoleId NVARCHAR(64) NULL, " +
            "ItemType NVARCHAR(32) NOT NULL, " +
            "Content NVARCHAR(MAX) NOT NULL, " +
            "MetadataJson NVARCHAR(MAX) NULL, " +
            "TraceId NVARCHAR(128) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchReportSnapshots', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchReportSnapshots(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchReportSnapshots PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnId BIGINT NOT NULL, " +
            "TriggeredByStageId BIGINT NULL, " +
            "VersionIndex INT NOT NULL, " +
            "IsFinal BIT NOT NULL, " +
            "ReportBlocksJson NVARCHAR(MAX) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchDecisionSnapshots', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchDecisionSnapshots(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchDecisionSnapshots PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnId BIGINT NOT NULL, " +
            "SupersededByDecisionId BIGINT NULL, " +
            "Rating NVARCHAR(32) NULL, " +
            "Action NVARCHAR(64) NULL, " +
            "ExecutiveSummary NVARCHAR(MAX) NULL, " +
            "InvestmentThesis NVARCHAR(MAX) NULL, " +
            "FinalDecisionJson NVARCHAR(MAX) NULL, " +
            "RiskConsensus NVARCHAR(MAX) NULL, " +
            "DissentJson NVARCHAR(MAX) NULL, " +
            "NextActionsJson NVARCHAR(MAX) NULL, " +
            "InvalidationConditionsJson NVARCHAR(MAX) NULL, " +
            "Confidence DECIMAL(18,2) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        // Indexes
        await EnsureIndexAsync(dbContext, "IX_ResearchSessions_SessionKey",
            "CREATE UNIQUE INDEX IX_ResearchSessions_SessionKey ON dbo.ResearchSessions(SessionKey);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchSessions_Symbol_Status",
            "CREATE INDEX IX_ResearchSessions_Symbol_Status ON dbo.ResearchSessions(Symbol, Status);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchSessions_Symbol_UpdatedAt",
            "CREATE INDEX IX_ResearchSessions_Symbol_UpdatedAt ON dbo.ResearchSessions(Symbol, UpdatedAt);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchTurns_SessionId_TurnIndex",
            "CREATE UNIQUE INDEX IX_ResearchTurns_SessionId_TurnIndex ON dbo.ResearchTurns(SessionId, TurnIndex);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchStageSnapshots_TurnId_StageType",
            "CREATE INDEX IX_ResearchStageSnapshots_TurnId_StageType ON dbo.ResearchStageSnapshots(TurnId, StageType, StageRunIndex);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchRoleStates_StageId_RoleId",
            "CREATE INDEX IX_ResearchRoleStates_StageId_RoleId ON dbo.ResearchRoleStates(StageId, RoleId, RunIndex);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchFeedItems_TurnId_CreatedAt",
            "CREATE INDEX IX_ResearchFeedItems_TurnId_CreatedAt ON dbo.ResearchFeedItems(TurnId, CreatedAt);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchReportSnapshots_SessionId_TurnId",
            "CREATE INDEX IX_ResearchReportSnapshots_SessionId_TurnId ON dbo.ResearchReportSnapshots(SessionId, TurnId, VersionIndex);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchDecisionSnapshots_SessionId_TurnId",
            "CREATE INDEX IX_ResearchDecisionSnapshots_SessionId_TurnId ON dbo.ResearchDecisionSnapshots(SessionId, TurnId);", cancellationToken);
    }

    private static async Task EnsureIndexAsync(AppDbContext dbContext, string indexName, string createSql, CancellationToken ct)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}') {createSql}", ct);
    }
}
