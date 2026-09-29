namespace PayFlow.Domain.Entities
{
    public class RefreshToken
    {
        private RefreshToken() { }

        public RefreshToken(
            Guid userId,
            string tokenHash,
            DateTime expiresAtUtc,
            Guid? familyId = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("O usuário é obrigatório.", nameof(userId));
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("O hash do token é obrigatório.", nameof(tokenHash));
            if (expiresAtUtc <= DateTime.UtcNow)
                throw new ArgumentException("A expiração deve estar no futuro.", nameof(expiresAtUtc));

            Id = Guid.NewGuid();
            UserId = userId;
            TokenHash = tokenHash;
            FamilyId = familyId ?? Id;
            CreatedAtUtc = DateTime.UtcNow;
            ExpiresAtUtc = expiresAtUtc;
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string TokenHash { get; private set; } = string.Empty;

        public Guid FamilyId { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime ExpiresAtUtc { get; private set; }

        public DateTime? RevokedAtUtc { get; private set; }

        public Guid? ReplacedByTokenId { get; private set; }

        public string? RevocationReason { get; private set; }

        public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

        public void Revoke(string reason, Guid? replacedByTokenId = null)
        {
            if (RevokedAtUtc is not null)
                return;

            RevokedAtUtc = DateTime.UtcNow;
            ReplacedByTokenId = replacedByTokenId;
            RevocationReason = reason;
        }
    }
}
