using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplerJiangAiAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStockQueryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockQueryHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TurnoverRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PeRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Speed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockQueryHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockQueryHistories_Symbol",
                table: "StockQueryHistories",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockQueryHistories");
        }
    }
}
