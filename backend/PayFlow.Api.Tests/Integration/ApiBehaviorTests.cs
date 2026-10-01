using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Accounts.DTOs;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Features.Transactions.DTOs;
using PayFlow.Infrastructure.Authentication;

namespace PayFlow.Api.Tests.Integration;

[Trait("Category", "Integration")]
public class ApiBehaviorTests
{
    [Theory]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    public async Task ProtectedEndpoint_ShouldRejectBearerToken_WhenValidationFails(string scenario)
    {
        using var factory = new ApiFactory();
        var user = new User("owner@example.com", "hash");
        using var client = factory.Client();
        var settings = factory.Jwt;
        if (scenario == "signature") settings.Key = Convert.ToBase64String(Enumerable.Repeat((byte)8, 32).ToArray());
        if (scenario == "issuer") settings.Issuer = "wrong-issuer";
        if (scenario == "audience") settings.Audience = "wrong-audience";
        string encoded;
        if (scenario == "expired")
        {
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(settings.Issuer, settings.Audience,
                [new System.Security.Claims.Claim("sub", user.Id.ToString())], expires: new DateTime(2000, 1, 1),
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Convert.FromBase64String(settings.Key)),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
            encoded = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
        else encoded = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(settings)).GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", encoded);

        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/accounts")]
    [InlineData("/api/auth/me")]
    public async Task ProtectedEndpoint_ShouldReturnUnauthorized_WhenBearerTokenIsMissing(string route)
    {
        using var factory = new ApiFactory();
        using var client = factory.Client();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAccount_ShouldBindStringEnumAndReturnRetrievableAccount_WhenTokenIsValid()
    {
        using var factory = new ApiFactory();
        var user = new User("owner@example.com", "hash");
        using var client = factory.Client(user);
        await factory.SeedAsync(user);

        var response = await client.PostAsJsonAsync("/api/accounts", new { holderName = "Business Owner", accountType = "Business" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var fetched = await client.GetAsync(response.Headers.Location);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await fetched.Content.ReadAsStringAsync();
        json.Should().Contain("\"accountType\":\"Business\"");
        json.Should().Contain(user.Id.ToString());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("\"Unknown\"")]
    public async Task CreateAccount_ShouldRejectInvalidEnumBinding_WhenEnumIsNumericOrUndefined(string enumJson)
    {
        using var factory = new ApiFactory();
        var user = new User("owner@example.com", "hash");
        using var client = factory.Client(user);
        await factory.SeedAsync(user);
        using var body = new StringContent("{\"holderName\":\"Owner\",\"accountType\":" + enumJson + "}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/accounts", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAccount_ShouldHideForeignAccount_WhenCallerIsNotOwner()
    {
        using var factory = new ApiFactory();
        var owner = new User("owner@example.com", "hash");
        var stranger = new User("stranger@example.com", "hash");
        var account = new Account(owner.Id, "Owner");
        using var client = factory.Client(stranger);
        await factory.SeedAsync(owner, stranger, account);

        var response = await client.GetAsync($"/api/accounts/{account.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Transfer_ShouldReturnCreatedTransactionAndMoveMoney_WhenRequestUsesDestinationKey()
    {
        using var factory = new ApiFactory();
        var owner = new User("owner@example.com", "hash");
        var receiver = new User("receiver@example.com", "hash");
        var source = new Account(owner.Id, "Source", 10m);
        var destination = new Account(receiver.Id, "Destination");
        destination.SetTransferKey(TransferKeyType.Email, "dest@example.com");
        using var client = factory.Client(owner);
        await factory.SeedAsync(owner, receiver, source, destination);

        var response = await client.PostAsJsonAsync($"/api/accounts/{source.Id}/transfers",
            new { destinationKeyType = "Email", destinationKey = "DEST@Example.com", amount = 2.01m });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await client.GetFromJsonAsync<TransactionResponse>(response.Headers.Location);
        transaction.Should().BeEquivalentTo(new { SourceAccountId = source.Id, DestinationAccountId = destination.Id, Amount = 2.01m });
        var account = await client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{source.Id}", factory.JsonOptions);
        account!.balance.Should().Be(7.99m);
    }

    [Fact]
    public async Task Transfer_ShouldReturnUnprocessableEntity_WhenBalanceIsInsufficient()
    {
        using var factory = new ApiFactory();
        var owner = new User("owner@example.com", "hash");
        var source = new Account(owner.Id, "Source", 1m);
        var destination = new Account(owner.Id, "Destination");
        destination.SetTransferKey(TransferKeyType.Email, "dest@example.com");
        using var client = factory.Client(owner);
        await factory.SeedAsync(owner, source, destination);

        var response = await client.PostAsJsonAsync($"/api/accounts/{source.Id}/transfers",
            new { destinationKeyType = "Email", destinationKey = "dest@example.com", amount = 2m });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{source.Id}", factory.JsonOptions))!.balance.Should().Be(1m);
    }

    [Fact]
    public async Task SetTransferKey_ShouldReturnConflict_WhenNormalizedKeyBelongsToAnotherAccount()
    {
        using var factory = new ApiFactory();
        var owner = new User("owner@example.com", "hash");
        var first = new Account(owner.Id, "First");
        var second = new Account(owner.Id, "Second");
        first.SetTransferKey(TransferKeyType.Email, "same@example.com");
        using var client = factory.Client(owner);
        await factory.SeedAsync(owner, first, second);

        var response = await client.PutAsJsonAsync($"/api/accounts/{second.Id}/transfer-key", new { type = "Email", value = "SAME@Example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{second.Id}", factory.JsonOptions))!.hasTransferKey.Should().BeFalse();
    }

    [Fact]
    public async Task SetTransferKey_ShouldNotExposeSensitiveInput_WhenKeyValidationFails()
    {
        using var factory = new ApiFactory();
        var owner = new User("owner@example.com", "hash");
        var account = new Account(owner.Id, "Owner");
        using var client = factory.Client(owner);
        await factory.SeedAsync(owner, account);

        var response = await client.PutAsJsonAsync($"/api/accounts/{account.Id}/transfer-key", new { type = "Cpf", value = "52998224726" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("52998224726");
    }

    [Fact]
    public async Task LoginAndRefresh_ShouldRejectPredecessorAfterRotation_WhenTokenIsReplayed()
    {
        using var factory = new ApiFactory();
        using var client = factory.Client();
        var user = new User("owner@example.com", new Argon2PasswordHasher().Hash("Valid123!"));
        await factory.SeedAsync(user);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "Valid123!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var original = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        var rotated = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = original.RefreshToken });
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        var successor = (await rotated.Content.ReadFromJsonAsync<LoginResponse>())!;

        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = original.RefreshToken });

        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var afterReplay = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = successor.RefreshToken });
        afterReplay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
