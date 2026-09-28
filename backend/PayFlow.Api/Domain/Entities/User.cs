namespace PayFlow.Domain.Entities
{
    public class User
    {
        private User() { }

        public User(string email, string passwordHash)
        {
            ValidateCredential(email);
            ValidateCredential(passwordHash);

            Id = Guid.NewGuid();
            Email = email.Trim();
            PasswordHash = passwordHash;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        public string Email { get; private set; } = string.Empty;

        public string PasswordHash { get; private set; } = string.Empty;

        public DateTime CreatedAtUtc { get; private set; }


        private static void ValidateCredential(string credential)
        {
            if (string.IsNullOrWhiteSpace(credential))
            {
                throw new ArgumentException(
                    "A credencial não pode ser nula, vazia ou conter apenas espaços.",
                    nameof(credential));
            }
        }
    }
}
