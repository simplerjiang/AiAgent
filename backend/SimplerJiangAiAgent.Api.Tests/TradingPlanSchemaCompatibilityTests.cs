using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SimplerJiangAiAgent.Api.Infrastructure.Jobs;
using SimplerJiangAiAgent.Api.Migrations;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class TradingPlanSchemaCompatibilityTests
{
    [Fact]
    public void BuildSqlServerAnalysisHistoryCompatibilitySql_DropsDefaultThenAltersColumnBeforeNormalizingLegacyZeros()
    {
        var sql = TradingPlanSchemaInitializer.BuildSqlServerAnalysisHistoryCompatibilitySql();

        var dropConstraintIndex = sql.IndexOf("DROP CONSTRAINT", StringComparison.Ordinal);
        var alterColumnIndex = sql.IndexOf(
            "ALTER TABLE dbo.TradingPlans ALTER COLUMN AnalysisHistoryId BIGINT NULL",
            StringComparison.Ordinal);
        var normalizeZerosIndex = sql.IndexOf(
            "UPDATE dbo.TradingPlans SET AnalysisHistoryId = NULL WHERE AnalysisHistoryId <= 0",
            StringComparison.Ordinal);

        Assert.True(dropConstraintIndex >= 0, sql);
        Assert.True(alterColumnIndex > dropConstraintIndex, sql);
        Assert.True(normalizeZerosIndex > alterColumnIndex, sql);
    }

    [Fact]
    public async Task EnsureAsync_CompatibilitySql_IncludesScenarioAndDateColumns()
    {
        var sql = await GetEnsureSqlAsync();

        Assert.Contains("ActiveScenario", sql);
        Assert.Contains("PlanStartDate", sql);
        Assert.Contains("PlanEndDate", sql);
    }

    [Fact]
    public void MakeTradingPlanAnalysisHistoryOptionalMigration_Up_AltersColumnBeforeNormalizingLegacyZeros()
    {
        var migration = new TestableMakeTradingPlanAnalysisHistoryOptionalMigration();
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        migration.InvokeUp(migrationBuilder);

        Assert.Collection(
            migrationBuilder.Operations,
            operation =>
            {
                var alterColumn = Assert.IsType<AlterColumnOperation>(operation);
                Assert.Equal("TradingPlans", alterColumn.Table);
                Assert.Equal("AnalysisHistoryId", alterColumn.Name);
                Assert.True(alterColumn.IsNullable);
            },
            operation =>
            {
                var sql = Assert.IsType<SqlOperation>(operation);
                Assert.Equal(
                    "UPDATE [TradingPlans] SET [AnalysisHistoryId] = NULL WHERE [AnalysisHistoryId] <= 0;",
                    sql.Sql);
            });
    }

    private sealed class TestableMakeTradingPlanAnalysisHistoryOptionalMigration : MakeTradingPlanAnalysisHistoryOptional
    {
        public void InvokeUp(MigrationBuilder migrationBuilder)
        {
            Up(migrationBuilder);
        }
    }

    private static async Task<string> GetEnsureSqlAsync()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<Data.AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new Data.AppDbContext(options);
        await TradingPlanSchemaInitializer.EnsureAsync(dbContext);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'TradingPlans';";
        return (await command.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }
}