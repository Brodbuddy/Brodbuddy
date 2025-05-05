using Microsoft.AspNetCore.Diagnostics;
using Core.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Api.Http.Middleware;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = exception switch
        {
            BusinessRuleViolationException => StatusCodes.Status400BadRequest,
            EntityNotFoundException => StatusCodes.Status404NotFound,
            AuthenticationException => StatusCodes.Status401Unauthorized,
            AuthorizationException => StatusCodes.Status403Forbidden,

            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = "An error occurred",
                Detail = exception.Message,
                Type = exception.GetType().Name,
            },
            Exception = exception
        };


        switch (exception)
        {
            case EntityNotFoundException entityNotFoundException:
                problemDetailsContext.ProblemDetails.Extensions["entityName"] = entityNotFoundException.EntityName;
                problemDetailsContext.ProblemDetails.Extensions["entityId"] = entityNotFoundException.EntityId;
                break;
            case AuthenticationException authException:
                problemDetailsContext.ProblemDetails.Extensions["failureReason"] = authException.FailureReason;
                break;
            case AuthorizationException authzException when !string.IsNullOrEmpty(authzException.RequiredPermission):
                problemDetailsContext.ProblemDetails.Extensions["requiredPermission"] =
                    authzException.RequiredPermission;
                break;
            case BusinessRuleViolationException businessRuleViolation:
                problemDetailsContext.ProblemDetails.Extensions["ruleViolated"] = businessRuleViolation.RuleName;
                break;
        }

        return await problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
}