using FluentValidation;
using PayFlow.Features.Accounts;
using PayFlow.Features.Accounts.Validators;
using PayFlow.Features.Authentication;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Transactions;
using PayFlow.Features.Users;
using PayFlow.Features.Users.Validators;
using PayFlow.Infrastructure.Authentication;

namespace PayFlow.Infrastructure.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {

            services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateDepositRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

            services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

            services.AddScoped<AuthService>();
            services.AddScoped<UserService>();
            services.AddScoped<AccountService>();
            services.AddScoped<TransactionService>();
            return services;
        }
    }
}
