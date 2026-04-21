using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplerJiangAiAgent.Api.Migrations
{
    public partial class MakeTradingPlanAnalysisHistoryOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "AnalysisHistoryId",
                table: "TradingPlans",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.Sql("UPDATE [TradingPlans] SET [AnalysisHistoryId] = NULL WHERE [AnalysisHistoryId] <= 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Rollback is not supported because manual trading plans can legitimately have no analysis history.");
        }
    }
}