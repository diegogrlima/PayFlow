using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("TB_RefreshTokens");

            builder.HasKey(token => token.Id);
            builder.Property(token => token.Id).ValueGeneratedNever();

            builder.Property(token => token.TokenHash)
                .HasMaxLength(64)
                .IsRequired();
            builder.HasIndex(token => token.TokenHash).IsUnique();

            builder.Property(token => token.FamilyId).IsRequired();
            builder.HasIndex(token => token.FamilyId);

            builder.Property(token => token.CreatedAtUtc).IsRequired();
            builder.Property(token => token.ExpiresAtUtc).IsRequired();
            builder.Property(token => token.RevocationReason).HasMaxLength(100);

            builder.Property(token => token.RevokedAtUtc)
                .IsConcurrencyToken();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<RefreshToken>()
                .WithMany()
                .HasForeignKey(token => token.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
