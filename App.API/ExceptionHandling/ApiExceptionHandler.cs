using App.Application.Exceptions;
using App.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace App.API.ExceptionHandling;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = exception switch
        {
            InvalidCredentialsException => StatusCodes.Status401Unauthorized,
            AccountDeactivatedException => StatusCodes.Status403Forbidden,
            UnauthorizedFriendshipActionException => StatusCodes.Status403Forbidden,
            NotFriendshipParticipantException => StatusCodes.Status403Forbidden,
            UserNotFoundException => StatusCodes.Status404NotFound,
            FriendshipNotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = httpContext.Response.StatusCode,
            Title = httpContext.Response.StatusCode >= 500
                ? "An unexpected error occurred."
                : "The request could not be processed.",
            Detail = httpContext.Response.StatusCode >= 500 ? null : exception.Message
        };

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });

        return true;
    }
}
