using Microsoft.OpenApi;

namespace PayFlow.Infrastructure.DependencyInjection
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddSwaggerConfiguration(
            this IServiceCollection services
            )
        {
            services.AddSwaggerGen(static option =>
            {
                option.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "PayFlow",
                        Description =
                       """
                                API REST para criação e consulta de contas, realização de 
                                transferências e consulta do histórico de transações. As operações 
                                validam saldo, contas envolvidas e valores antes de concluir cada 
                                transferência.
                        """,
                        Version = "v1",
                        Contact = new OpenApiContact
                        {
                            Name = "Diego Lima",
                            Email = "...",
                            Url = new Uri("https://github.com/diegogrlima")
                        },
                    }
                    );

                option.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Informe apenas o access token JWT."
                    });

                option.AddSecurityRequirement(document =>
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
        {

            app.UseSwagger();
            app.UseSwaggerUI();

            return app;

        }
    }
}
