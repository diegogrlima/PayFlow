using Microsoft.EntityFrameworkCore;
using PayFlow.Data;
using PayFlow.Repositories;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Configurations
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {

            var connectionString = configuration.GetConnectionString("PayFlowDatabase")
                ?? throw new InvalidOperationException("Connection string 'PayFlowDatabase' não foi encontrada.");

            services.AddDbContext<PayFlowDbContext>(
                    options => options.UseSqlServer(connectionString)
                );

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            return services;
        }
    }
}
