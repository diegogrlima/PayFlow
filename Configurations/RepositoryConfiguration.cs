using PayFlow.Repositories;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Configurations
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            return services;
        }
    }
}
