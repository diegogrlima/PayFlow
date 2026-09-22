using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PayFlow.Exceptions
{
    public class ApiExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problem = exception switch
            {
                ValidationException validation => new ValidationProblemDetails(
                    validation.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage)
                        .ToArray()))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dados inválidos"
                },

                ResourceNotFoundException => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Recurso não foi encontrado",
                    Detail = exception.Message
                },

                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Error Interno"
                }
            };

            context.Response.StatusCode = problem.Status!.Value;
            await context.Response.WriteAsJsonAsync(
                    problem,
                    cancellationToken
            );

            return true;
        }
    }
}
