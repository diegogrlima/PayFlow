using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Authentication;
using PayFlow.Infrastructure.Persistence;

namespace PayFlow.Api.Tests.Integration;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    public System.Text.Json.JsonSerializerOptions JsonOptions { get; } = new(System.Text.Json.JsonSerializerDefaults.Web)
    { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public ApiFactory()
    {
        connection.Open();
        connection.CreateFunction<string, int>("LEN", value => value.Length);
    }

    public JwtSettings Jwt => new()
    {
        Key = Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray()),
        Issuer = "integration-tests", Audience = "integration-client", ExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PayFlowDatabase", "Server=unused;Database=unused");
        builder.UseSetting("Jwt:Key", Jwt.Key);
        builder.UseSetting("Jwt:Issuer", Jwt.Issuer);
        builder.UseSetting("Jwt:Audience", Jwt.Audience);
        builder.UseSetting("Jwt:ExpirationMinutes", "15");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<PayFlowDbContext>();
            services.RemoveAll<DbContextOptions<PayFlowDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PayFlowDbContext>>();
            services.AddDbContext<PayFlowDbContext>(options => options.UseSqlite(connection));
        });
    }

    public HttpClient Client(User? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PayFlowDbContext>().Database.EnsureCreated();
        if (user is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtTokenService(Options.Create(Jwt)).GenerateToken(user));
        return client;
    }

    public async Task SeedAsync(params object[] entities)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}
