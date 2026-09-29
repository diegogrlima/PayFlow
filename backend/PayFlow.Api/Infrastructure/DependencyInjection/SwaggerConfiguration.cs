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
                option.TagActionsBy(api =>
                [
                    SwaggerDocumentationFilter.GetTag(
                        api.ActionDescriptor.RouteValues["controller"])
                ]);

                option.OrderActionsBy(api =>
                    $"{SwaggerDocumentationFilter.GetTagOrder(api.ActionDescriptor.RouteValues["controller"]):D2}_" +
                    $"{api.RelativePath}_{api.HttpMethod}");

                option.OperationFilter<SwaggerDocumentationFilter>();
                option.DocumentFilter<SwaggerDocumentationFilter>();

                option.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "PayFlow API",
                        Description =
                       """
                                API REST para gerenciamento de usuários, autenticação, contas e
                                transferências financeiras. Para acessar as operações protegidas,
                                autentique-se e informe o access token no botão Authorize.
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
