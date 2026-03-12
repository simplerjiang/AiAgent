using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

public static class LocalFactSchemaInitializer
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;
        if (!provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('dbo.LocalStockNews','U') IS NULL BEGIN " +
            "CREATE TABLE dbo.LocalStockNews (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, Symbol NVARCHAR(450) NOT NULL, Name NVARCHAR(MAX) NOT NULL, SectorName NVARCHAR(MAX) NULL, Title NVARCHAR(MAX) NOT NULL, Category NVARCHAR(128) NOT NULL, Source NVARCHAR(256) NOT NULL, SourceTag NVARCHAR(128) NOT NULL, ExternalId NVARCHAR(450) NULL, PublishTime DATETIME2 NOT NULL, CrawledAt DATETIME2 NOT NULL, Url NVARCHAR(MAX) NULL); " +
            "CREATE INDEX IX_LocalStockNews_Symbol_PublishTime ON dbo.LocalStockNews(Symbol, PublishTime); " +
            "CREATE INDEX IX_LocalStockNews_Symbol_SourceTag ON dbo.LocalStockNews(Symbol, SourceTag); END;",
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('dbo.LocalSectorReports','U') IS NULL BEGIN " +
            "CREATE TABLE dbo.LocalSectorReports (Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, Symbol NVARCHAR(450) NULL, SectorName NVARCHAR(MAX) NULL, Level NVARCHAR(64) NOT NULL, Title NVARCHAR(MAX) NOT NULL, Source NVARCHAR(256) NOT NULL, SourceTag NVARCHAR(128) NOT NULL, ExternalId NVARCHAR(450) NULL, PublishTime DATETIME2 NOT NULL, CrawledAt DATETIME2 NOT NULL, Url NVARCHAR(MAX) NULL); " +
            "CREATE INDEX IX_LocalSectorReports_Symbol_Level_PublishTime ON dbo.LocalSectorReports(Symbol, Level, PublishTime); " +
            "CREATE INDEX IX_LocalSectorReports_Level_PublishTime ON dbo.LocalSectorReports(Level, PublishTime); END;",
            cancellationToken);
    }
}