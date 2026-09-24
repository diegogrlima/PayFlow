using FluentValidation;
using PayFlow.DTOs.Account.Validators;
using PayFlow.DTOs.Users.Validators;
using PayFlow.Services;
using PayFlow.Services.Interfaces;

namespace PayFlow.Configurations
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {

            services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateDepositRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

            services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

            services.AddScoped<UserService>();
            services.AddScoped<AccountService>();
            services.AddScoped<TransactionService>();
            return services;
        }
    }
}
