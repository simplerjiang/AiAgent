using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplerJiangAiAgent.Api.Migrations
{
    public partial class AddTradingPlanEventsForStep43 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
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
                "IF COL_LENGTH('dbo.TradingPlanEvents','Symbol') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Symbol NVARCHAR(32) NOT NULL CONSTRAINT DF_TradingPlanEvents_Symbol DEFAULT(''); " +
                "IF COL_LENGTH('dbo.TradingPlanEvents','EventType') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD EventType NVARCHAR(32) NOT NULL CONSTRAINT DF_TradingPlanEvents_EventType DEFAULT('VolumeDivergenceWarning'); " +
                "IF COL_LENGTH('dbo.TradingPlanEvents','Severity') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Severity NVARCHAR(16) NOT NULL CONSTRAINT DF_TradingPlanEvents_Severity DEFAULT('Warning'); " +
                "IF COL_LENGTH('dbo.TradingPlanEvents','Message') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD Message NVARCHAR(MAX) NOT NULL CONSTRAINT DF_TradingPlanEvents_Message DEFAULT(''); " +
                "IF COL_LENGTH('dbo.TradingPlanEvents','SnapshotPrice') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD SnapshotPrice DECIMAL(18,2) NULL; " +
                "IF COL_LENGTH('dbo.TradingPlanEvents','MetadataJson') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD MetadataJson NVARCHAR(MAX) NULL; " +
                "IF COL_LENGTH('dbo.TradingPlanEvents','OccurredAt') IS NULL ALTER TABLE dbo.TradingPlanEvents ADD OccurredAt DATETIME2 NOT NULL CONSTRAINT DF_TradingPlanEvents_OccurredAt DEFAULT(SYSUTCDATETIME()); " +
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlanEvents_PlanId_OccurredAt' AND object_id = OBJECT_ID('dbo.TradingPlanEvents')) CREATE INDEX IX_TradingPlanEvents_PlanId_OccurredAt ON dbo.TradingPlanEvents(PlanId, OccurredAt); " +
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradingPlanEvents_Symbol_OccurredAt' AND object_id = OBJECT_ID('dbo.TradingPlanEvents')) CREATE INDEX IX_TradingPlanEvents_Symbol_OccurredAt ON dbo.TradingPlanEvents(Symbol, OccurredAt);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradingPlanEvents");
        }
    }
}