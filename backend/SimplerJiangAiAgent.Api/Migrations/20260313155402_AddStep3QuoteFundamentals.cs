using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplerJiangAiAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStep3QuoteFundamentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "StockQuoteSnapshots",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StockQuoteSnapshots",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "FloatMarketCap",
                table: "StockQuoteSnapshots",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PeRatio",
                table: "StockQuoteSnapshots",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SectorName",
                table: "StockQuoteSnapshots",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShareholderCount",
                table: "StockQuoteSnapshots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeRatio",
                table: "StockQuoteSnapshots",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StockCompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SectorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ShareholderCount = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCompanyProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockCompanyProfiles_Symbol",
                table: "StockCompanyProfiles",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockCompanyProfiles");

            migrationBuilder.DropColumn(
                name: "FloatMarketCap",
                table: "StockQuoteSnapshots");

            migrationBuilder.DropColumn(
                name: "PeRatio",
                table: "StockQuoteSnapshots");

            migrationBuilder.DropColumn(
                name: "SectorName",
                table: "StockQuoteSnapshots");

            migrationBuilder.DropColumn(
                name: "ShareholderCount",
                table: "StockQuoteSnapshots");

            migrationBuilder.DropColumn(
                name: "VolumeRatio",
                table: "StockQuoteSnapshots");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "StockQuoteSnapshots",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StockQuoteSnapshots",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);
        }
    }
}
