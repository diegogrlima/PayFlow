using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Transactions;
using PayFlow.Features.Transactions.Validators;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayFlow.Infrastructure.DependencyInjection;

namespace PayFlow.Api.Tests.Integration;

[Trait("Category", "SqlServer")]
public class SqlServerBehaviorTests
{
    [Fact]
    public async Task DuplicateKey_ShouldNotExposeFullCpfInLogs_WhenSqlServerRejectsUniqueIndexWrite()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        await using (var setup = database.Context())
        {
            var existing = await setup.Accounts.SingleAsync(x => x.Id == seed.Destination.Id);
            existing.SetTransferKey(TransferKeyType.Cpf, "52998224725");
            await setup.SaveChangesAsync();
        }
        using var logs = new CapturedLogs();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:PayFlowDatabase"] = database.ConnectionString }).Build();
        services.AddDatabaseConfiguration(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();
        var account = await db.Accounts.SingleAsync(x => x.Id == seed.Source.Id);
        account.SetTransferKey(TransferKeyType.Cpf, "52998224725");

        Func<Task> act = () => new AccountRepository(db).UpdateAsync(account);

        await act.Should().ThrowAsync<ConflictException>();
        logs.Messages.Should().NotBeEmpty("the capture must receive real EF diagnostics");
        string.Join("\n", logs.Messages).Should().NotContain("52998224725");
    }

    [Theory]
    [InlineData("sent", 2, 4)]
    [InlineData("received", 1, 3)]
    [InlineData(null, 3, 4)]
    public async Task History_ShouldFilterOrderAndPageRelatedTransfers_WhenRepositoryQueriesSqlServer(string? type, int firstAmount, int secondAmount)
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        await using var db = database.Context();
        var unrelated = new Account(seed.Owner.Id, "Unrelated");
        var another = new Account(seed.Owner.Id, "Another");
        db.AddRange(unrelated, another);
        var transfers = new[]
        {
            new Transaction(seed.Destination.Id, seed.Source.Id, 1m),
            new Transaction(seed.Source.Id, seed.Destination.Id, 2m),
            new Transaction(seed.Destination.Id, seed.Source.Id, 3m),
            new Transaction(seed.Source.Id, seed.Destination.Id, 4m),
            new Transaction(unrelated.Id, another.Id, 5m)
        };
        for (var i = 0; i < transfers.Length; i++)
        {
            db.Add(transfers[i]);
            db.Entry(transfers[i]).Property(x => x.CreatedAtUtc).CurrentValue = new DateTime(2020, 1, 1).AddMinutes(i);
        }
        await db.SaveChangesAsync();
        var repository = new TransactionRepository(db);

        var page1 = await repository.GetHistoryByAccountIdAsync(seed.Source.Id, type, 1, 1);
        var page2 = await repository.GetHistoryByAccountIdAsync(seed.Source.Id, type, 2, 1);

        page1.Should().ContainSingle().Which.Amount.Should().Be(secondAmount);
        page2.Should().ContainSingle().Which.Amount.Should().Be(firstAmount);
    }

    [Fact]
    public async Task ListAccounts_ShouldExcludeOtherOwners_WhenRepositoryQueriesSqlServer()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        await using var db = database.Context();
        var other = new User("other@example.com", "hash");
        db.AddRange(other, new Account(other.Id, "Foreign"));
        await db.SaveChangesAsync();

        var accounts = await new AccountRepository(db).GetByUserIdAsync(seed.Owner.Id);

        accounts.Select(account => account.Id).Should().BeEquivalentTo([seed.Source.Id, seed.Destination.Id]);
    }

    [Fact]
    public async Task EmailLookup_ShouldTrimInput_WhenRepositoryQueriesSqlServer()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        await using var db = database.Context();
        var repository = new UserRepository(db);

        var found = await repository.GetByEmailAsync("  owner@example.com  ");
        var exists = await repository.ExistsByEmailAsync("  owner@example.com  ");

        found!.Id.Should().Be(seed.Owner.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Migrations_ShouldPreserveEnumValues_WhenUpgradedAndDowngradedWithExistingData()
    {
        await using var database = await SqlTestDatabase.CreateAsync("20261001094648_AddAccountTransferKeys");
        await using var db = database.Context();
        var user = Guid.NewGuid();
        var account = Guid.NewGuid();
        var created = new DateTime(2020, 1, 1);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO TB_Users (Id, Email, PasswordHash, CreatedAtUtc) VALUES ({user}, {"owner@example.com"}, {"hash"}, {created})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO TB_Accounts (Id, UserId, HolderName, Balance, CreatedAtUtc, AccountType, TransferKeyType, TransferKey) VALUES ({account}, {user}, {"Business"}, {10m}, {created}, {2}, {4}, {"04252011000110"})");

        await db.GetService<IMigrator>().MigrateAsync();

        var materialized = (await db.Accounts.AsNoTracking().SingleAsync())!;
        materialized.AccountType.Should().Be(AccountType.Business);
        materialized.TransferKeyType.Should().Be(TransferKeyType.Cnpj);
        materialized.TransferKey.Should().Be("04252011000110");
        await db.GetService<IMigrator>().MigrateAsync("20261001094648_AddAccountTransferKeys");
        var numeric = await db.Database.SqlQueryRaw<int>("SELECT AccountType AS Value FROM TB_Accounts").SingleAsync();
        numeric.Should().Be(2);
        var keyType = await db.Database.SqlQueryRaw<int>("SELECT TransferKeyType AS Value FROM TB_Accounts").SingleAsync();
        keyType.Should().Be(4);
    }

    [Fact]
    public async Task TransferKey_ShouldRejectOneWriter_WhenTwoAccountsRegisterSameKeyConcurrently()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        await using var first = database.Context();
        await using var second = database.Context();
        var a = await first.Accounts.SingleAsync(x => x.Id == seed.Source.Id);
        var b = await second.Accounts.SingleAsync(x => x.Id == seed.Destination.Id);
        a.SetTransferKey(TransferKeyType.Email, "shared@example.com");
        b.SetTransferKey(TransferKeyType.Email, "shared@example.com");

        var errors = await Task.WhenAll(Capture(() => new AccountRepository(first).UpdateAsync(a)),
            Capture(() => new AccountRepository(second).UpdateAsync(b)));

        errors.Count(error => error is null).Should().Be(1);
        errors.OfType<ConflictException>().Should().ContainSingle();
        await using var verify = database.Context();
        (await verify.Accounts.CountAsync(x => x.TransferKey == "shared@example.com")).Should().Be(1);
    }

    [Fact]
    public async Task Constraints_ShouldRejectUnpairedKeyFields_WhenDirectSqlBypassesDomain()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        await using var db = database.Context();

        Func<Task> act = () => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE TB_Accounts SET TransferKeyType = {"Cpf"} WHERE Id = {seed.Source.Id}");

        await act.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        (await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == seed.Source.Id)).TransferKeyType.Should().BeNull();
    }

    [Fact]
    public async Task Transfer_ShouldReturnConflictAndPreserveMoney_WhenSerializableWritersDeadlock()
    {
        await using var database = await SqlTestDatabase.CreateAsync();
        var seed = await database.SeedAccountsAsync();
        var gate = new ConcurrentSaveGate();
        await using var first = database.Context(gate);
        await using var second = database.Context(gate);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var request = new PayFlow.Features.Transactions.DTOs.CreateTransactionRequest(TransferKeyType.Email, "dest@example.com", 10m);

        var errors = await Task.WhenAll(Capture(() => Service(first, seed.Owner.Id).AddTransactionAsync(seed.Source.Id, request, timeout.Token)),
            Capture(() => Service(second, seed.Owner.Id).AddTransactionAsync(seed.Source.Id, request, timeout.Token)));

        errors.Count(error => error is null).Should().Be(1);
        errors.OfType<ConflictException>().Should().ContainSingle("a real SQL deadlock must be translated to a retryable conflict");
        await using var verify = database.Context();
        (await verify.Accounts.SingleAsync(x => x.Id == seed.Source.Id)).Balance.Should().Be(90m);
        (await verify.Accounts.SingleAsync(x => x.Id == seed.Destination.Id)).Balance.Should().Be(10m);
        (await verify.Transactions.CountAsync()).Should().Be(1);
    }

    private static TransactionService Service(PayFlowDbContext db, Guid owner) => new(new TransactionRepository(db),
        new AccountRepository(db), new UnitOfWork(db), new CurrentUser(owner), new CreateTransactionRequestValidator());

    private static async Task<Exception?> Capture(Func<Task> operation)
    {
        try { await operation(); return null; }
        catch (Exception error) { return error; }
    }

    private sealed class CurrentUser(Guid id) : ICurrentUser
    { public Guid UserId => id; public bool IsAuthenticated => true; }

    // Each test owns this gate; both real transactions finish reading before either writes.
    private sealed class ConcurrentSaveGate : SaveChangesInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            return result;
        }
    }
}
