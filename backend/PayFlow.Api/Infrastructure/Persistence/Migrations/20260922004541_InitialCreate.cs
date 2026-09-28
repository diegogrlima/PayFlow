using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayFlow.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HolderName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Accounts", x => x.Id);
                    table.CheckConstraint("CK_TB_Accounts_Balance_NonNegative", "[Balance] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "TB_Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Transactions", x => x.Id);
                    table.CheckConstraint("CK_TB_Transactions_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_TB_Transactions_Different_Accounts", "[SourceAccountId] <> [DestinationAccountId]");
                    table.ForeignKey(
                        name: "FK_TB_Transactions_TB_Accounts_DestinationAccountId",
                        column: x => x.DestinationAccountId,
                        principalTable: "TB_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TB_Transactions_TB_Accounts_SourceAccountId",
                        column: x => x.SourceAccountId,
                        principalTable: "TB_Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_Transactions_CreatedAtUtc",
                table: "TB_Transactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Transactions_DestinationAccountId",
                table: "TB_Transactions",
                column: "DestinationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Transactions_SourceAccountId",
                table: "TB_Transactions",
                column: "SourceAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_Transactions");

            migrationBuilder.DropTable(
                name: "TB_Accounts");
        }
    }
}
