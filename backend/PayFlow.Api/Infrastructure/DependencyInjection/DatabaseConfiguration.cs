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
                        // SQL duplicate-key exceptions can contain the full transfer key.
                        // Surface the sanitized API exception instead of logging the raw EF failure.
                        .ConfigureWarnings(warnings => warnings.Ignore(
                            Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError,
                            Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SaveChangesFailed))
                );

            return services;
        }
    }
}
