using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplerJiangAiAgent.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockAgentAnalysisHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Interval = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UseInternet = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAgentAnalysisHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockChatSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockChatMessages_StockChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StockChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAgentAnalysisHistories_Symbol_CreatedAt",
                table: "StockAgentAnalysisHistories",
                columns: new[] { "Symbol", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockChatMessages_SessionId",
                table: "StockChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockChatSessions_SessionKey",
                table: "StockChatSessions",
                column: "SessionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockChatSessions_Symbol_UpdatedAt",
                table: "StockChatSessions",
                columns: new[] { "Symbol", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAgentAnalysisHistories");

            migrationBuilder.DropTable(
                name: "StockChatMessages");

            migrationBuilder.DropTable(
                name: "StockChatSessions");
        }
    }
}
