using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplerJiangAiAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalFactTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalSectorReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SectorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceTag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CrawledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalSectorReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalStockNews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SectorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceTag = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CrawledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalStockNews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalSectorReports_Level_PublishTime",
                table: "LocalSectorReports",
                columns: new[] { "Level", "PublishTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalSectorReports_Symbol_Level_PublishTime",
                table: "LocalSectorReports",
                columns: new[] { "Symbol", "Level", "PublishTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalStockNews_Symbol_PublishTime",
                table: "LocalStockNews",
                columns: new[] { "Symbol", "PublishTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalStockNews_Symbol_SourceTag",
                table: "LocalStockNews",
                columns: new[] { "Symbol", "SourceTag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalSectorReports");

            migrationBuilder.DropTable(
                name: "LocalStockNews");
        }
    }
}
