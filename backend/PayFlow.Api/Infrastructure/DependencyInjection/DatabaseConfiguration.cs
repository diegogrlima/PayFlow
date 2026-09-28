using Microsoft.EntityFrameworkCore;
using PayFlow.Infrastructure.Persistence;

namespace PayFlow.Infrastructure.DependencyInjection
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

            return services;
        }
    }
}
