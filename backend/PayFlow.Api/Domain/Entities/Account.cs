namespace PayFlow.Domain.Entities
{
    public class Account
    {
        private Account()
        {
        }

        public Account(Guid userId, string holderName, decimal initialBalance = 0, AccountType accountType = AccountType.Individual)
        {
            ValidateUserId(userId);
            ValidateHolderName(holderName);
            ValidateBalance(initialBalance);

            if (!Enum.IsDefined(accountType)) throw new ArgumentException("Tipo de conta inv\u00e1lido.");
            AccountType = accountType;
            Id = Guid.NewGuid();
            UserId = userId;
            HolderName = holderName.Trim();
            Balance = initialBalance;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public AccountType AccountType { get; private set; }
        public TransferKeyType? TransferKeyType { get; private set; }
        public string? TransferKey { get; private set; }

        public void SetTransferKey(TransferKeyType type, string value)
        {
            if ((AccountType == AccountType.Individual && type == global::PayFlow.Domain.Entities.TransferKeyType.Cnpj)
                || (AccountType == AccountType.Business && type == global::PayFlow.Domain.Entities.TransferKeyType.Cpf))
                throw new ArgumentException("Chave incompat\u00edvel com o tipo da conta.");
            var normalized = TransferKeyNormalizer.Normalize(type, value);
            TransferKeyType = type;
            TransferKey = normalized;
        }

        public void RemoveTransferKey()
        {
            TransferKeyType = null;
            TransferKey = null;
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public User User { get; private set; } = null!;

        public string HolderName { get; private set; } = string.Empty;

        public decimal Balance { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public void Update(string holderName, decimal balance)
        {
            ValidateHolderName(holderName);
            ValidateBalance(balance);

            HolderName = holderName.Trim();
            Balance = balance;
        }

        private static void ValidateHolderName(string holderName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(holderName);

            if (holderName.Trim().Length < 3 || holderName.Trim().Length > 150)
            {
                throw new ArgumentException(
                    "O nome do titular deve ter entre 3 e 150 caracteres.",
                    nameof(holderName));
            }
        }

        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "O identificador do usuário é obrigatório.",
                    nameof(userId));
            }
        }

        private static void ValidateBalance(decimal balance)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(balance),
                    "O saldo não pode ser negativo.");
            }
        }

        public void Deposit(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "O valor do depósito deve ser maior que zero.");
            }

            Balance += amount;
        }

        public void Debit(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "O valor do débito deve ser maior que zero.");
            }

            if (amount > Balance)
            {
                throw new InvalidOperationException(
                    "Saldo insuficiente para realizar o débito.");
            }

            Balance -= amount;
        }
    }
}
