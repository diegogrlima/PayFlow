using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable(
                "TB_Transactions",
                table =>
                {
                    table.HasCheckConstraint(
                        "CK_TB_Transactions_Amount_Positive",
                        "[Amount] > 0");
                    table.HasCheckConstraint(
                        "CK_TB_Transactions_Different_Accounts",
                        "[SourceAccountId] <> [DestinationAccountId]");
                });

            builder.HasKey(transaction => transaction.Id);

            builder.Property(transaction => transaction.Id)
                .ValueGeneratedNever();

            builder.Property(transaction => transaction.SourceAccountId)
                .IsRequired();

            builder.Property(transaction => transaction.DestinationAccountId)
                .IsRequired();

            builder.Property(transaction => transaction.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(transaction => transaction.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(transaction => transaction.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(transaction => transaction.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(transaction => transaction.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(transaction => transaction.SourceAccountId);
            builder.HasIndex(transaction => transaction.DestinationAccountId);
            builder.HasIndex(transaction => transaction.CreatedAtUtc);
        }
    }
}
