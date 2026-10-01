using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Persistence;

namespace PayFlow.Api.Tests.Integration;

internal sealed class SqlTestDatabase : IAsyncDisposable
{
    private readonly string name = "PayFlowTests_" + Guid.NewGuid().ToString("N");
    private readonly string connectionString;
    public string ConnectionString => connectionString;

    private SqlTestDatabase()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PAYFLOW_TEST_SQLSERVER")
            ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true")
        { InitialCatalog = name, ConnectTimeout = 15, Pooling = false };
        connectionString = connection.ConnectionString;
    }

    public PayFlowDbContext Context(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<PayFlowDbContext>()
        .UseSqlServer(connectionString, sql => sql.CommandTimeout(30)).AddInterceptors(interceptors).Options);

    public static async Task<SqlTestDatabase> CreateAsync(string? migration = null)
    {
        var database = new SqlTestDatabase();
        try
        {
            await using var context = database.Context();
            await context.GetService<IMigrator>().MigrateAsync(migration);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public async Task<(User Owner, Account Source, Account Destination)> SeedAccountsAsync()
    {
        await using var db = Context();
        var user = new User("owner@example.com", "hash");
        var source = new Account(user.Id, "Source", 100m);
        var destination = new Account(user.Id, "Destination");
        destination.SetTransferKey(TransferKeyType.Email, "dest@example.com");
        db.AddRange(user, source, destination);
        await db.SaveChangesAsync();
        return (user, source, destination);
    }

    public async ValueTask DisposeAsync()
    {
        // The database name is generated here and never taken from application configuration.
        var target = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (target != name || !target.StartsWith("PayFlowTests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to remove a database outside this test's ownership.");
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }
}
