using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("TB_Accounts");

            builder.Property(a => a.AccountType).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(a => a.TransferKeyType).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.TransferKey).HasMaxLength(254);
            builder.HasIndex(a => a.TransferKey).IsUnique().HasFilter("[TransferKey] IS NOT NULL");
            builder.ToTable("TB_Accounts", table => {
                table.HasCheckConstraint("CK_TB_Accounts_AccountType", "[AccountType] IN ('Individual', 'Business')");
                table.HasCheckConstraint("CK_TB_Accounts_TransferKey", "([TransferKeyType] IS NULL AND [TransferKey] IS NULL) OR ([TransferKeyType] IN ('Email', 'Cpf', 'Phone', 'Cnpj') AND [TransferKeyType] IS NOT NULL AND [TransferKey] IS NOT NULL AND LEN([TransferKey]) > 0)");
            });
            builder.HasKey(account => account.Id);

            builder.Property(account => account.Id)
                .ValueGeneratedNever();

            builder.Property(account => account.HolderName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(account => account.Balance)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(account => account.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(account => account.User)
                .WithMany(user => user.Accounts)
                .HasForeignKey(account => account.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(
                "TB_Accounts",
                table => table.HasCheckConstraint(
                    "CK_TB_Accounts_Balance_NonNegative",
                    "[Balance] >= 0"));
        }
    }
}
