using FluentValidation;
using PayFlow.DTOs.Account.Validators;
using PayFlow.Services;

namespace PayFlow.Configurations
{
    public static class ServiceConfiguration
    {
        public static  IServiceCollection AddServices(this IServiceCollection services)
        {

            services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateDepositRequestValidator>();

            services.AddScoped<AccountService>();
            return services;
        }
    }
}
