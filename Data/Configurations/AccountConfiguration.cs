using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Entities;

namespace PayFlow.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("TB_Accounts");

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

            builder.ToTable(
                "TB_Accounts",
                table => table.HasCheckConstraint(
                    "CK_TB_Accounts_Balance_NonNegative",
                    "[Balance] >= 0"));
        }
    }
}
