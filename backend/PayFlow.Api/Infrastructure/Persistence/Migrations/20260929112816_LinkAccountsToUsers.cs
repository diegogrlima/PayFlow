using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkAccountsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "TB_Accounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TB_Accounts_UserId",
                table: "TB_Accounts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TB_Accounts_TB_Users_UserId",
                table: "TB_Accounts",
                column: "UserId",
                principalTable: "TB_Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TB_Accounts_TB_Users_UserId",
                table: "TB_Accounts");

            migrationBuilder.DropIndex(
                name: "IX_TB_Accounts_UserId",
                table: "TB_Accounts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TB_Accounts");
        }
    }
}
