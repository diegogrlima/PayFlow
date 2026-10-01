using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Accounts;
using PayFlow.Features.Accounts.Validators;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Transactions;
using PayFlow.Features.Transactions.DTOs;
using PayFlow.Features.Transactions.Validators;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Repositories;

namespace PayFlow.Api.Tests;
public class TransferKeyTests
{
    [Theory]
    [InlineData(TransferKeyType.Email, "  USER@Example.com ", "user@example.com")]
    [InlineData(TransferKeyType.Cpf, "529.982.247-25", "52998224725")]
    [InlineData(TransferKeyType.Cnpj, "04.252.011/0001-10", "04252011000110")]
    [InlineData(TransferKeyType.Phone, "+55 (11) 99999-9999", "+5511999999999")]
    public void NormalizesValidKeys(TransferKeyType type, string input, string expected)
        => Assert.Equal(expected, TransferKeyNormalizer.Normalize(type, input));

    [Theory]
    [InlineData(TransferKeyType.Cpf, "11111111111")]
    [InlineData(TransferKeyType.Cpf, "52998224726")]
    [InlineData(TransferKeyType.Cnpj, "04252011000111")]
    [InlineData(TransferKeyType.Email, "invalid")]
    [InlineData(TransferKeyType.Phone, "11999999999")]
    [InlineData(TransferKeyType.Phone, "+5511000000000")]
    [InlineData((TransferKeyType)99, "value")]
    public void RejectsInvalidKeys(TransferKeyType type, string value)
        => Assert.Throws<ArgumentException>(() => TransferKeyNormalizer.Normalize(type, value));

    [Fact]
    public void KeysRespectAccountTypeAndRemovalClearsBothFields()
    {
        var personal = new Account(Guid.NewGuid(), "Personal");
        Assert.Throws<ArgumentException>(() => personal.SetTransferKey(TransferKeyType.Cnpj, "04252011000110"));
        var business = new Account(Guid.NewGuid(), "Business", accountType: AccountType.Business);
        Assert.Throws<ArgumentException>(() => business.SetTransferKey(TransferKeyType.Cpf, "52998224725"));
        personal.SetTransferKey(TransferKeyType.Cpf, "52998224725");
        Assert.Equal("*******4725", TransferKeyNormalizer.Mask(personal.TransferKeyType, personal.TransferKey));
        personal.RemoveTransferKey();
        Assert.Null(personal.TransferKey);
        Assert.Null(personal.TransferKeyType);
    }

    [Fact]
    public async Task TransfersByNormalizedKeyAndChecksOwnershipAndBalance()
    {
        using var fixture = new Fixture();
        var request = new CreateTransactionRequest(TransferKeyType.Email, " DEST@Example.com ", 25);
        var result = await fixture.Transfers.AddTransactionAsync(fixture.Source.Id, request, default);
        Assert.Equal(fixture.Destination.Id, result.DestinationAccountId);
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(75, (await fixture.Db.Accounts.FindAsync(fixture.Source.Id))!.Balance);
        Assert.Equal(25, (await fixture.Db.Accounts.FindAsync(fixture.Destination.Id))!.Balance);
        Assert.Equal(1, await fixture.Db.Transactions.CountAsync());
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("missing")]
    [InlineData("self")]
    [InlineData("balance")]
    [InlineData("amount")]
    public async Task FailedTransfersDoNotChangeBalancesOrCreateTransactions(string scenario)
    {
        using var fixture = new Fixture();
        var key = scenario == "missing" ? "missing@example.com" : "dest@example.com";
        if (scenario == "self") { fixture.Source.SetTransferKey(TransferKeyType.Email, "self@example.com"); await fixture.Db.SaveChangesAsync(); key = "self@example.com"; }
        var source = scenario == "foreign" ? fixture.Destination.Id : fixture.Source.Id;
        var amount = scenario == "balance" ? 101 : scenario == "amount" ? 0 : 10;
        var action = () => fixture.Transfers.AddTransactionAsync(source, new CreateTransactionRequest(TransferKeyType.Email, key, amount), default);
        if (scenario is "foreign" or "missing") await Assert.ThrowsAsync<ResourceNotFoundException>(action);
        else if (scenario == "amount") await Assert.ThrowsAsync<FluentValidation.ValidationException>(action);
        else await Assert.ThrowsAsync<BusinessRuleException>(action);
        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(100, (await fixture.Db.Accounts.FindAsync(fixture.Source.Id))!.Balance);
        Assert.Equal(0, (await fixture.Db.Accounts.FindAsync(fixture.Destination.Id))!.Balance);
        Assert.Empty(await fixture.Db.Transactions.ToListAsync());
    }

    [Fact]
    public async Task ListsOnlyOwnedAccountsAndMasksKeysAndRejectsDuplicates()
    {
        using var f = new Fixture();
        var service = f.Accounts;
        var response = await service.SetTransferKeyAsync(f.Source.Id, new(TransferKeyType.Cpf, "529.982.247-25"), default);
        Assert.Equal("*******4725", response.transferKey);
        Assert.True(response.hasTransferKey);
        Assert.Single(await service.GetAllAsync());
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.RemoveTransferKeyAsync(f.Destination.Id, default));
        await Assert.ThrowsAsync<ConflictException>(() => service.SetTransferKeyAsync(f.Source.Id, new(TransferKeyType.Email, "DEST@Example.com"), default));
        await service.RemoveTransferKeyAsync(f.Source.Id, default);
        Assert.False((await service.GetByIdAsync(f.Source.Id))!.hasTransferKey);
    }

    [Fact]
    public async Task DatabaseAllowsNullKeysButEnforcesUniqueKeysAndPairedFields()
    {
        using var f = new Fixture();
        f.Source.SetTransferKey(TransferKeyType.Email, "dest@example.com");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
        f.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<SqliteException>(() => f.Db.Database.ExecuteSqlRawAsync("UPDATE TB_Accounts SET TransferKeyType = 1 WHERE TransferKey IS NULL"));
    }

    [Fact]
    public async Task InvalidKeyProducesValidationErrorWithoutIncludingInput()
    {
        using var f = new Fixture();
        var error = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            f.Transfers.AddTransactionAsync(f.Source.Id, new(TransferKeyType.Cpf, "52998224726", 10), default));
        Assert.Single(error.Errors);
        Assert.DoesNotContain("52998224726", error.ToString());
    }

    [Fact]
    public async Task CanReplaceAndRemoveKeyWithoutChangingBalance()
    {
        using var f = new Fixture();
        await f.Accounts.SetTransferKeyAsync(f.Source.Id, new(TransferKeyType.Email, "first@example.com"), default);
        await f.Accounts.SetTransferKeyAsync(f.Source.Id, new(TransferKeyType.Email, "second@example.com"), default);
        f.Db.ChangeTracker.Clear();
        var account = (await f.Db.Accounts.FindAsync(f.Source.Id))!;
        Assert.Equal("second@example.com", account.TransferKey);
        Assert.Equal(100, account.Balance);
        await f.Accounts.RemoveTransferKeyAsync(f.Source.Id, default);
        await f.Accounts.RemoveTransferKeyAsync(f.Source.Id, default);
        Assert.Null(account.TransferKey);
    }

    [Fact]
    public async Task PersistenceFailureRollsBackDebitCreditAndTransaction()
    {
        using var f = new Fixture();
        f.SaveFailure.Enabled = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Transfers.AddTransactionAsync(
            f.Source.Id, new(TransferKeyType.Email, "dest@example.com", 10), default));
        f.Db.ChangeTracker.Clear();
        Assert.Equal(100, (await f.Db.Accounts.FindAsync(f.Source.Id))!.Balance);
        Assert.Equal(0, (await f.Db.Accounts.FindAsync(f.Destination.Id))!.Balance);
        Assert.Empty(await f.Db.Transactions.ToListAsync());
    }

    [Fact]
    public async Task PersistsEnumNamesAsTextAndMaterializesEnums()
    {
        using var f = new Fixture();
        f.Source.SetTransferKey(TransferKeyType.Cpf, "52998224725");
        await f.Db.SaveChangesAsync();
        using var command = f.Db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT AccountType, TransferKeyType FROM TB_Accounts WHERE Id = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = f.Source.Id;
        command.Parameters.Add(parameter);
        using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Individual", reader.GetString(0));
            Assert.Equal("Cpf", reader.GetString(1));
        }
        f.Db.ChangeTracker.Clear();
        var account = (await f.Db.Accounts.FindAsync(f.Source.Id))!;
        Assert.Equal(AccountType.Individual, account.AccountType);
        Assert.Equal(TransferKeyType.Cpf, account.TransferKeyType);
    }

    private sealed class FailAfterSave : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<int> SavedChangesAsync(Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (Enabled) throw new InvalidOperationException("Injected persistence failure.");
            return ValueTask.FromResult(result);
        }
    }

    private class CurrentUser(Guid id) : ICurrentUser
    { public Guid UserId => id; public bool IsAuthenticated => true; }
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public FailAfterSave SaveFailure { get; } = new();
        public PayFlowDbContext Db { get; }
        public Account Source { get; }
        public Account Destination { get; }
        public TransactionService Transfers { get; }
        public AccountService Accounts { get; }
        public Fixture()
        {
            connection.Open();
            connection.CreateFunction<string, int>("LEN", s => s.Length);
            Db = new(new DbContextOptionsBuilder<PayFlowDbContext>().UseSqlite(connection).AddInterceptors(SaveFailure).Options);
            Db.Database.EnsureCreated();
            var owner = new User("owner@example.com", "hash");
            var receiver = new User("receiver@example.com", "hash");
            Source = new(owner.Id, "Source", 100);
            Destination = new(receiver.Id, "Destination");
            Destination.SetTransferKey(TransferKeyType.Email, "dest@example.com");
            Db.AddRange(owner, receiver, Source, Destination);
            Db.SaveChanges();
            var repository = new AccountRepository(Db);
            var current = new CurrentUser(owner.Id);
            Transfers = new(new TransactionRepository(Db), repository, new UnitOfWork(Db), current, new CreateTransactionRequestValidator());
            Accounts = new(repository, new UserRepository(Db), current, new CreateAccountRequestValidator(), new CreateDepositRequestValidator());
        }
        public void Dispose() { Db.Dispose(); connection.Dispose(); }
    }
}
