namespace PayFlow.Configurations
{
    public static class JwtConfiguration
    {
        public static IServiceCollection AddJwtConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtSettings>(
                configuration.GetSection("Jwt"));

            return services;
        }
    }
}
