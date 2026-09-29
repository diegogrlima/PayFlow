using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PayFlow.Infrastructure.DependencyInjection
{
    public class SwaggerDocumentationFilter : IOperationFilter, IDocumentFilter
    {
        private static readonly IReadOnlyDictionary<string, (int Order, string Name, string Description)> Tags =
            new Dictionary<string, (int, string, string)>
            {
                ["Users"] = (1, "Usuários", "Cadastro e consulta dos dados do usuário."),
                ["Auth"] = (2, "Autenticação", "Login, renovação e revogação de tokens."),
                ["Account"] = (3, "Contas", "Criação, consulta, depósito e histórico de contas."),
                ["Transaction"] = (4, "Transações", "Transferências entre contas e consulta de transações.")
            };

        private static readonly IReadOnlyDictionary<(string Controller, string Action), (string Summary, string Description)> Operations =
            new Dictionary<(string, string), (string, string)>
            {
                [("Users", "Create")] = ("Cadastrar usuário", "Cria um novo usuário. Esta operação é pública."),
                [("Auth", "Login")] = ("Autenticar usuário", "Valida as credenciais e retorna os tokens de acesso e renovação."),
                [("Auth", "Refresh")] = ("Renovar tokens", "Rotaciona o refresh token e retorna um novo par de tokens."),
                [("Auth", "Revoke")] = ("Revogar refresh token", "Invalida um refresh token. A operação é idempotente."),
                [("Auth", "GetCurrent")] = ("Consultar o usuário autenticado", "Retorna os dados do usuário identificado pelo access token."),
                [("Account", "Create")] = ("Criar conta", "Cria uma conta vinculada ao usuário autenticado."),
                [("Account", "GetById")] = ("Consultar conta", "Retorna uma conta pertencente ao usuário autenticado."),
                [("Account", "AddDeposit")] = ("Realizar depósito", "Adiciona saldo a uma conta pertencente ao usuário autenticado."),
                [("Account", "GetAllTransactions")] = ("Consultar histórico da conta", "Lista as transações de uma conta do usuário autenticado."),
                [("Transaction", "AddTransaction")] = ("Realizar transferência", "Transfere saldo de uma conta do usuário para outra conta."),
                [("Transaction", "GetById")] = ("Consultar transferência", "Retorna uma transferência da qual o usuário participa.")
            };

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor action)
                return;

            if (!Operations.TryGetValue((action.ControllerName, action.ActionName), out var documentation))
                return;

            operation.Summary = documentation.Summary;
            operation.Description = documentation.Description;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags = Tags.Values
                .OrderBy(tag => tag.Order)
                .Select(tag => new OpenApiTag
                {
                    Name = tag.Name,
                    Description = tag.Description
                })
                .ToHashSet();
        }

        public static string GetTag(string? controllerName)
        {
            return controllerName is not null && Tags.TryGetValue(controllerName, out var tag)
                ? tag.Name
                : controllerName ?? "Outros";
        }

        public static int GetTagOrder(string? controllerName)
        {
            return controllerName is not null && Tags.TryGetValue(controllerName, out var tag)
                ? tag.Order
                : int.MaxValue;
        }
    }
}
