using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public static class SourceGovernanceSchemaInitializer
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;

        if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID('dbo.NewsSourceRegistries','U') IS NULL BEGIN " +
                "CREATE TABLE dbo.NewsSourceRegistries (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, Domain NVARCHAR(450) NOT NULL, BaseUrl NVARCHAR(MAX) NOT NULL, Tier NVARCHAR(64) NOT NULL, Status NVARCHAR(64) NOT NULL, FetchStrategy NVARCHAR(64) NOT NULL, ParserVersion NVARCHAR(64) NOT NULL, QualityScore DECIMAL(18,2) NULL, ParseSuccessRate DECIMAL(18,2) NULL, TimestampCoverage DECIMAL(18,2) NULL, FreshnessLagMinutes INT NULL, ConsecutiveFailures INT NOT NULL DEFAULT(0), LastSuccessAt DATETIME2 NULL, LastCheckedAt DATETIME2 NULL, LastStatusReason NVARCHAR(MAX) NULL, CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), UpdatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())); " +
                "CREATE UNIQUE INDEX IX_NewsSourceRegistries_Domain ON dbo.NewsSourceRegistries(Domain); " +
                "CREATE INDEX IX_NewsSourceRegistries_Status_Tier ON dbo.NewsSourceRegistries(Status, Tier); END;",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID('dbo.NewsSourceHealthDailies','U') IS NULL BEGIN " +
                "CREATE TABLE dbo.NewsSourceHealthDailies (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, SourceId BIGINT NOT NULL, HealthDate DATETIME2 NOT NULL, ParseSuccessRate DECIMAL(18,2) NOT NULL, TimestampCoverage DECIMAL(18,2) NOT NULL, DuplicateRate DECIMAL(18,2) NOT NULL, FreshnessLagMinutes INT NOT NULL, ErrorCount INT NOT NULL, SuggestedStatus NVARCHAR(64) NOT NULL, SuggestionReason NVARCHAR(MAX) NULL, CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())); " +
                "CREATE UNIQUE INDEX IX_NewsSourceHealthDailies_SourceId_HealthDate ON dbo.NewsSourceHealthDailies(SourceId, HealthDate); END;",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID('dbo.NewsSourceCandidates','U') IS NULL BEGIN " +
                "CREATE TABLE dbo.NewsSourceCandidates (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, Domain NVARCHAR(450) NOT NULL, HomepageUrl NVARCHAR(MAX) NOT NULL, ProposedTier NVARCHAR(64) NOT NULL, Status NVARCHAR(64) NOT NULL, DiscoveryReason NVARCHAR(MAX) NOT NULL, FetchStrategy NVARCHAR(64) NOT NULL, VerificationScore DECIMAL(18,2) NULL, ParseSuccessRate DECIMAL(18,2) NULL, TimestampCoverage DECIMAL(18,2) NULL, FreshnessLagMinutes INT NULL, DiscoveredAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), VerifiedAt DATETIME2 NULL); " +
                "CREATE INDEX IX_NewsSourceCandidates_Domain_Status ON dbo.NewsSourceCandidates(Domain, Status); END;",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID('dbo.NewsSourceVerificationRuns','U') IS NULL BEGIN " +
                "CREATE TABLE dbo.NewsSourceVerificationRuns (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, TraceId NVARCHAR(128) NULL, SourceId BIGINT NULL, CandidateId BIGINT NULL, Domain NVARCHAR(450) NOT NULL, Success BIT NOT NULL, HttpStatusCode INT NOT NULL, ParseSuccessRate DECIMAL(18,2) NOT NULL, TimestampCoverage DECIMAL(18,2) NOT NULL, DuplicateRate DECIMAL(18,2) NOT NULL DEFAULT(0), ContentDepth DECIMAL(18,2) NOT NULL DEFAULT(0), CrossSourceAgreement DECIMAL(18,2) NOT NULL DEFAULT(0), FreshnessLagMinutes INT NOT NULL, VerificationScore DECIMAL(18,2) NOT NULL, FailureReason NVARCHAR(MAX) NULL, ExecutedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())); " +
                "CREATE INDEX IX_NewsSourceVerificationRuns_Domain_ExecutedAt ON dbo.NewsSourceVerificationRuns(Domain, ExecutedAt); END;",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF COL_LENGTH('dbo.NewsSourceVerificationRuns','DuplicateRate') IS NULL ALTER TABLE dbo.NewsSourceVerificationRuns ADD DuplicateRate DECIMAL(18,2) NOT NULL CONSTRAINT DF_NewsSourceVerificationRuns_DuplicateRate DEFAULT(0); " +
                "IF COL_LENGTH('dbo.NewsSourceVerificationRuns','ContentDepth') IS NULL ALTER TABLE dbo.NewsSourceVerificationRuns ADD ContentDepth DECIMAL(18,2) NOT NULL CONSTRAINT DF_NewsSourceVerificationRuns_ContentDepth DEFAULT(0); " +
                "IF COL_LENGTH('dbo.NewsSourceVerificationRuns','CrossSourceAgreement') IS NULL ALTER TABLE dbo.NewsSourceVerificationRuns ADD CrossSourceAgreement DECIMAL(18,2) NOT NULL CONSTRAINT DF_NewsSourceVerificationRuns_CrossSourceAgreement DEFAULT(0); " +
                "IF COL_LENGTH('dbo.NewsSourceVerificationRuns','TraceId') IS NULL ALTER TABLE dbo.NewsSourceVerificationRuns ADD TraceId NVARCHAR(128) NULL; " +
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_NewsSourceVerificationRuns_TraceId' AND object_id=OBJECT_ID('dbo.NewsSourceVerificationRuns')) CREATE INDEX IX_NewsSourceVerificationRuns_TraceId ON dbo.NewsSourceVerificationRuns(TraceId);",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID('dbo.CrawlerChangeQueues','U') IS NULL BEGIN " +
                "CREATE TABLE dbo.CrawlerChangeQueues (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, TraceId NVARCHAR(128) NULL, SourceId BIGINT NOT NULL, Domain NVARCHAR(450) NOT NULL, Status NVARCHAR(64) NOT NULL, TriggerReason NVARCHAR(MAX) NOT NULL, ProposedFilesJson NVARCHAR(MAX) NULL, ProposedPatchJson NVARCHAR(MAX) NULL, ProposedPatchSummary NVARCHAR(MAX) NULL, ProposedTestCommand NVARCHAR(MAX) NULL, ProposedReplayCommand NVARCHAR(MAX) NULL, ValidationNote NVARCHAR(MAX) NULL, DeploymentBackupJson NVARCHAR(MAX) NULL, CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), UpdatedAt DATETIME2 NULL); " +
                "CREATE INDEX IX_CrawlerChangeQueues_SourceId_Status ON dbo.CrawlerChangeQueues(SourceId, Status); END;",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF COL_LENGTH('dbo.CrawlerChangeQueues','ProposedPatchJson') IS NULL ALTER TABLE dbo.CrawlerChangeQueues ADD ProposedPatchJson NVARCHAR(MAX) NULL; " +
                "IF COL_LENGTH('dbo.CrawlerChangeQueues','ProposedReplayCommand') IS NULL ALTER TABLE dbo.CrawlerChangeQueues ADD ProposedReplayCommand NVARCHAR(MAX) NULL; " +
                "IF COL_LENGTH('dbo.CrawlerChangeQueues','DeploymentBackupJson') IS NULL ALTER TABLE dbo.CrawlerChangeQueues ADD DeploymentBackupJson NVARCHAR(MAX) NULL; " +
                "IF COL_LENGTH('dbo.CrawlerChangeQueues','TraceId') IS NULL ALTER TABLE dbo.CrawlerChangeQueues ADD TraceId NVARCHAR(128) NULL; " +
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_CrawlerChangeQueues_TraceId' AND object_id=OBJECT_ID('dbo.CrawlerChangeQueues')) CREATE INDEX IX_CrawlerChangeQueues_TraceId ON dbo.CrawlerChangeQueues(TraceId);",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF OBJECT_ID('dbo.CrawlerChangeRuns','U') IS NULL BEGIN " +
                "CREATE TABLE dbo.CrawlerChangeRuns (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, TraceId NVARCHAR(128) NULL, QueueId BIGINT NOT NULL, Domain NVARCHAR(450) NOT NULL, Result NVARCHAR(64) NOT NULL, Note NVARCHAR(MAX) NULL, ExecutedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME())); " +
                "CREATE INDEX IX_CrawlerChangeRuns_QueueId_ExecutedAt ON dbo.CrawlerChangeRuns(QueueId, ExecutedAt); END;",
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "IF COL_LENGTH('dbo.CrawlerChangeRuns','TraceId') IS NULL ALTER TABLE dbo.CrawlerChangeRuns ADD TraceId NVARCHAR(128) NULL; " +
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_CrawlerChangeRuns_TraceId' AND object_id=OBJECT_ID('dbo.CrawlerChangeRuns')) CREATE INDEX IX_CrawlerChangeRuns_TraceId ON dbo.CrawlerChangeRuns(TraceId);",
                cancellationToken);
        }
    }
}