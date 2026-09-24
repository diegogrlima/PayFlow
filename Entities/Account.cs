namespace PayFlow.Entities
{
    public class Account
    {
        private Account()
        {
        }

        public Account(string holderName, decimal initialBalance = 0)
        {
            ValidateHolderName(holderName);
            ValidateBalance(initialBalance);

            Id = Guid.NewGuid();
            HolderName = holderName.Trim();
            Balance = initialBalance;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

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
