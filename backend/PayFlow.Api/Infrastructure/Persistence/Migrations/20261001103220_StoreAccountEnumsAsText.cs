using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreAccountEnumsAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TB_Accounts_AccountType",
                table: "TB_Accounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TB_Accounts_TransferKey",
                table: "TB_Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "TransferKeyType",
                table: "TB_Accounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccountType",
                table: "TB_Accounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("""
                UPDATE [TB_Accounts]
                SET [AccountType] = CASE [AccountType]
                    WHEN '1' THEN 'Individual' WHEN '2' THEN 'Business' ELSE [AccountType] END,
                    [TransferKeyType] = CASE [TransferKeyType]
                    WHEN '1' THEN 'Email' WHEN '2' THEN 'Cpf'
                    WHEN '3' THEN 'Phone' WHEN '4' THEN 'Cnpj' ELSE [TransferKeyType] END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TB_Accounts_AccountType",
                table: "TB_Accounts",
                sql: "[AccountType] IN ('Individual', 'Business')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TB_Accounts_TransferKey",
                table: "TB_Accounts",
                sql: "([TransferKeyType] IS NULL AND [TransferKey] IS NULL) OR ([TransferKeyType] IN ('Email', 'Cpf', 'Phone', 'Cnpj') AND [TransferKeyType] IS NOT NULL AND [TransferKey] IS NOT NULL AND LEN([TransferKey]) > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TB_Accounts_AccountType",
                table: "TB_Accounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TB_Accounts_TransferKey",
                table: "TB_Accounts");

            migrationBuilder.Sql("""
                UPDATE [TB_Accounts]
                SET [AccountType] = CASE [AccountType]
                    WHEN 'Individual' THEN '1' WHEN 'Business' THEN '2' ELSE [AccountType] END,
                    [TransferKeyType] = CASE [TransferKeyType]
                    WHEN 'Email' THEN '1' WHEN 'Cpf' THEN '2'
                    WHEN 'Phone' THEN '3' WHEN 'Cnpj' THEN '4' ELSE [TransferKeyType] END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TransferKeyType",
                table: "TB_Accounts",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccountType",
                table: "TB_Accounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TB_Accounts_AccountType",
                table: "TB_Accounts",
                sql: "[AccountType] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TB_Accounts_TransferKey",
                table: "TB_Accounts",
                sql: "([TransferKeyType] IS NULL AND [TransferKey] IS NULL) OR ([TransferKeyType] IN (1, 2, 3, 4) AND [TransferKeyType] IS NOT NULL AND [TransferKey] IS NOT NULL AND LEN([TransferKey]) > 0)");
        }
    }
}
