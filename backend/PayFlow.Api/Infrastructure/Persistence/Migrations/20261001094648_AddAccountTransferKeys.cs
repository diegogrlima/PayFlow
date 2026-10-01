using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTransferKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "TB_Accounts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TransferKey",
                table: "TB_Accounts",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransferKeyType",
                table: "TB_Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_Accounts_TransferKey",
                table: "TB_Accounts",
                column: "TransferKey",
                unique: true,
                filter: "[TransferKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TB_Accounts_AccountType",
                table: "TB_Accounts",
                sql: "[AccountType] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TB_Accounts_TransferKey",
                table: "TB_Accounts",
                sql: "([TransferKeyType] IS NULL AND [TransferKey] IS NULL) OR ([TransferKeyType] IN (1, 2, 3, 4) AND [TransferKeyType] IS NOT NULL AND [TransferKey] IS NOT NULL AND LEN([TransferKey]) > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TB_Accounts_TransferKey",
                table: "TB_Accounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TB_Accounts_AccountType",
                table: "TB_Accounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TB_Accounts_TransferKey",
                table: "TB_Accounts");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "TB_Accounts");

            migrationBuilder.DropColumn(
                name: "TransferKey",
                table: "TB_Accounts");

            migrationBuilder.DropColumn(
                name: "TransferKeyType",
                table: "TB_Accounts");
        }
    }
}
