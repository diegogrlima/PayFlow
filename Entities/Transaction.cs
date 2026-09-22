namespace PayFlow.Entities
{
    public class Transaction
    {
        private Transaction()
        {
        }

        public Transaction(
            Guid sourceAccountId,
            Guid destinationAccountId,
            decimal amount)
        {
            ValidateAccounts(sourceAccountId, destinationAccountId);
            ValidateAmount(amount);

            Id = Guid.NewGuid();
            SourceAccountId = sourceAccountId;
            DestinationAccountId = destinationAccountId;
            Amount = amount;
            Status = TransactionStatus.Completed;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        public Guid SourceAccountId { get; private set; }

        public Guid DestinationAccountId { get; private set; }

        public decimal Amount { get; private set; }

        public TransactionStatus Status { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        private static void ValidateAccounts(
            Guid sourceAccountId,
            Guid destinationAccountId)
        {
            if (sourceAccountId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A conta de origem deve ser informada.",
                    nameof(sourceAccountId));
            }

            if (destinationAccountId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A conta de destino deve ser informada.",
                    nameof(destinationAccountId));
            }

            if (sourceAccountId == destinationAccountId)
            {
                throw new ArgumentException(
                    "A conta de origem deve ser diferente da conta de destino.",
                    nameof(destinationAccountId));
            }
        }

        private static void ValidateAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "O valor da transferência deve ser maior que zero.");
            }

            if (decimal.Round(amount, 2) != amount)
            {
                throw new ArgumentException(
                    "O valor da transferência deve possuir no máximo duas casas decimais.",
                    nameof(amount));
            }
        }
    }
}
