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

        // R5 – Debate, Risk, Proposal tables
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchDebateMessages', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchDebateMessages(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchDebateMessages PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnId BIGINT NOT NULL, " +
            "StageId BIGINT NOT NULL, " +
            "Side NVARCHAR(20) NOT NULL, " +
            "RoleId NVARCHAR(64) NOT NULL, " +
            "RoundIndex INT NOT NULL, " +
            "Claim NVARCHAR(MAX) NOT NULL, " +
            "SupportingEvidenceRefsJson NVARCHAR(MAX) NULL, " +
            "CounterTargetRole NVARCHAR(64) NULL, " +
            "CounterPointsJson NVARCHAR(MAX) NULL, " +
            "OpenQuestionsJson NVARCHAR(MAX) NULL, " +
            "LlmTraceId NVARCHAR(256) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchManagerVerdicts', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchManagerVerdicts(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchManagerVerdicts PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnId BIGINT NOT NULL, " +
            "StageId BIGINT NOT NULL, " +
            "RoundIndex INT NOT NULL, " +
            "AdoptedBullPointsJson NVARCHAR(MAX) NULL, " +
            "AdoptedBearPointsJson NVARCHAR(MAX) NULL, " +
            "ShelvedDisputesJson NVARCHAR(MAX) NULL, " +
            "ResearchConclusion NVARCHAR(MAX) NULL, " +
            "InvestmentPlanDraftJson NVARCHAR(MAX) NULL, " +
            "IsConverged BIT NOT NULL DEFAULT 0, " +
            "LlmTraceId NVARCHAR(256) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchTraderProposals', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchTraderProposals(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchTraderProposals PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnId BIGINT NOT NULL, " +
            "StageId BIGINT NOT NULL, " +
            "Version INT NOT NULL, " +
            "Status NVARCHAR(20) NOT NULL, " +
            "Direction NVARCHAR(64) NULL, " +
            "EntryPlanJson NVARCHAR(MAX) NULL, " +
            "ExitPlanJson NVARCHAR(MAX) NULL, " +
            "PositionSizingJson NVARCHAR(MAX) NULL, " +
            "Rationale NVARCHAR(MAX) NULL, " +
            "SupersededByProposalId BIGINT NULL, " +
            "LlmTraceId NVARCHAR(256) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.ResearchRiskAssessments', N'U') IS NULL " +
            "CREATE TABLE dbo.ResearchRiskAssessments(" +
            "Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ResearchRiskAssessments PRIMARY KEY, " +
            "SessionId BIGINT NOT NULL, " +
            "TurnId BIGINT NOT NULL, " +
            "StageId BIGINT NOT NULL, " +
            "RoleId NVARCHAR(64) NOT NULL, " +
            "Tier NVARCHAR(20) NOT NULL, " +
            "RoundIndex INT NOT NULL, " +
            "RiskLimitsJson NVARCHAR(MAX) NULL, " +
            "InvalidationsJson NVARCHAR(MAX) NULL, " +
            "ProposalAssessment NVARCHAR(MAX) NULL, " +
            "AnalysisContent NVARCHAR(MAX) NULL, " +
            "ResponseToArtifactId BIGINT NULL, " +
            "LlmTraceId NVARCHAR(256) NULL, " +
            "CreatedAt DATETIME2 NOT NULL);", cancellationToken);

        await EnsureIndexAsync(dbContext, "IX_ResearchDebateMessages_Session_Turn_Stage_Round",
            "CREATE INDEX IX_ResearchDebateMessages_Session_Turn_Stage_Round ON dbo.ResearchDebateMessages(SessionId, TurnId, StageId, RoundIndex);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchManagerVerdicts_Session_Turn_Stage",
            "CREATE INDEX IX_ResearchManagerVerdicts_Session_Turn_Stage ON dbo.ResearchManagerVerdicts(SessionId, TurnId, StageId);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchTraderProposals_Session_Turn_Version",
            "CREATE INDEX IX_ResearchTraderProposals_Session_Turn_Version ON dbo.ResearchTraderProposals(SessionId, TurnId, Version);", cancellationToken);
        await EnsureIndexAsync(dbContext, "IX_ResearchRiskAssessments_Session_Turn_Role_Round",
            "CREATE INDEX IX_ResearchRiskAssessments_Session_Turn_Role_Round ON dbo.ResearchRiskAssessments(SessionId, TurnId, StageId, RoleId, RoundIndex);", cancellationToken);

        // ── R6: Report blocks ────────────────────────────────────────
        await dbContext.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'dbo.ResearchReportBlocks', N'U') IS NULL
            CREATE TABLE dbo.ResearchReportBlocks (
                Id              BIGINT          IDENTITY(1,1) PRIMARY KEY,
                SessionId       BIGINT          NOT NULL REFERENCES dbo.ResearchSessions(Id) ON DELETE CASCADE,
                TurnId          BIGINT          NOT NULL,
                BlockType       NVARCHAR(30)    NOT NULL,
                VersionIndex    INT             NOT NULL DEFAULT 0,
                Headline        NVARCHAR(500)   NULL,
                Summary         NVARCHAR(MAX)   NULL,
                KeyPointsJson           NVARCHAR(MAX) NULL,
                EvidenceRefsJson        NVARCHAR(MAX) NULL,
                CounterEvidenceRefsJson NVARCHAR(MAX) NULL,
                DisagreementsJson       NVARCHAR(MAX) NULL,
                RiskLimitsJson          NVARCHAR(MAX) NULL,
                InvalidationsJson       NVARCHAR(MAX) NULL,
                RecommendedActionsJson  NVARCHAR(MAX) NULL,
                Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
                DegradedFlagsJson       NVARCHAR(MAX) NULL,
                MissingEvidence         NVARCHAR(MAX) NULL,
                ConfidenceImpact        NVARCHAR(50)  NULL,
                SourceStageType         NVARCHAR(50)  NULL,
                SourceArtifactId        BIGINT        NULL,
                CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
                UpdatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
            );", cancellationToken);

        await EnsureIndexAsync(dbContext, "UQ_ResearchReportBlocks_Turn_Block_Version",
            "CREATE UNIQUE INDEX UQ_ResearchReportBlocks_Turn_Block_Version ON dbo.ResearchReportBlocks(TurnId, BlockType, VersionIndex);", cancellationToken);

        // R6: Add missing columns to ResearchDecisionSnapshots
        await dbContext.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('dbo.ResearchDecisionSnapshots','SupportingEvidenceJson') IS NULL
                ALTER TABLE dbo.ResearchDecisionSnapshots ADD SupportingEvidenceJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.ResearchDecisionSnapshots','CounterEvidenceJson') IS NULL
                ALTER TABLE dbo.ResearchDecisionSnapshots ADD CounterEvidenceJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH('dbo.ResearchDecisionSnapshots','ConfidenceExplanation') IS NULL
                ALTER TABLE dbo.ResearchDecisionSnapshots ADD ConfidenceExplanation NVARCHAR(MAX) NULL;", cancellationToken);
    }

    private static async Task EnsureIndexAsync(AppDbContext dbContext, string indexName, string createSql, CancellationToken ct)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}') {createSql}", ct);
    }
}
