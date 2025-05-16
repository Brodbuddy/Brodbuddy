using Microsoft.AspNetCore.Diagnostics;
using Core.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Http.Middleware;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            ValidationException => StatusCodes.Status400BadRequest, // FluentValidator exception
            BusinessRuleViolationException => StatusCodes.Status400BadRequest,
            AuthorizationException => StatusCodes.Status403Forbidden,
            EntityNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        LogException(exception, httpContext);
        
        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = GetTitleForException(exception),
                Detail = GetDetailForException(exception),
                Type = exception.GetType().Name,
                Instance = httpContext.Request.Path,
                Extensions = { ["traceId"] = httpContext.TraceIdentifier }
            },
            Exception = exception
        };
        
        switch (exception)
        {
            case ArgumentException argEx:
                problemDetailsContext.ProblemDetails.Extensions["parameterName"] = argEx.ParamName;
                break;
            case ValidationException validationException:
                problemDetailsContext.ProblemDetails.Extensions["errors"] = validationException.Errors.Select(e => new 
                { 
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                });
                break;
            case EntityNotFoundException entityNotFoundException:
                problemDetailsContext.ProblemDetails.Extensions["entityName"] = entityNotFoundException.EntityName;
                problemDetailsContext.ProblemDetails.Extensions["entityId"] = entityNotFoundException.EntityId;
                break;
            case AuthorizationException authzException when !string.IsNullOrEmpty(authzException.RequiredPermission):
                problemDetailsContext.ProblemDetails.Extensions["requiredPermission"] = authzException.RequiredPermission;
                break;
            case BusinessRuleViolationException businessRuleViolation:
                problemDetailsContext.ProblemDetails.Extensions["ruleViolated"] = businessRuleViolation.RuleName;
                break;
        }

        return await problemDetailsService.TryWriteAsync(problemDetailsContext);
    }
    
    private void LogException(Exception exception, HttpContext httpContext)
    {
        switch (exception)
        {
            case ValidationException:
            case BusinessRuleViolationException:
            case AuthorizationException:
                logger.LogWarning(
                    "API Security/Validation Event: {ExceptionType}. Path: {Path}, TraceId: {TraceId}",
                    exception.GetType().Name,
                    httpContext.Request.Path,
                    httpContext.TraceIdentifier);
                break;
                
            case EntityNotFoundException ex:
                logger.LogInformation(
                    "Resource Not Found. Path: {Path}, EntityName: {EntityName}, EntityId: {EntityId}, TraceId: {TraceId}",
                    httpContext.Request.Path,
                    ex.EntityName,
                    ex.EntityId,
                    httpContext.TraceIdentifier);
                break;
                
            default:
                logger.LogError(exception,
                    "Unhandled Exception. Path: {Path}, TraceId: {TraceId}",
                    httpContext.Request.Path,
                    httpContext.TraceIdentifier);
                break;
        }
    }
    
    private static string GetTitleForException(Exception exception) => exception switch
    {
        ArgumentNullException => "Required Parameter Missing",
        ArgumentOutOfRangeException => "Parameter Value Out of Range",
        ArgumentException => "Invalid Parameter",
        ValidationException => "Validation Failed",
        BusinessRuleViolationException => "Business Rule Violation",
        EntityNotFoundException => "Resource Not Found",
        AuthorizationException => "Forbidden Access",
        _ => "An Internal Error Occurred"
    };
    
    private static string GetDetailForException(Exception exception) => exception switch
    {
        ArgumentNullException e => $"The required parameter '{e.ParamName}' was not provided.",
        ArgumentOutOfRangeException e => $"The value for parameter '{e.ParamName}' is outside the acceptable range.",
        ArgumentException e when !string.IsNullOrEmpty(e.ParamName) => $"The parameter '{e.ParamName}' is invalid.",
        ValidationException => "One or more validation errors occurred.",
        EntityNotFoundException e => $"The {e.EntityName} with ID {e.EntityId} was not found.",
        BusinessRuleViolationException e => $"The operation violates business rule: {e.RuleName}",
        _ => exception.Message
    };
}