using App.Application.Exceptions;
using App.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationException or ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            NotFoundException or UserNotFoundException or FriendshipNotFoundException or CommunityNotFoundException =>
                (StatusCodes.Status404NotFound, "Not found", exception.Message),
            InvalidCredentialsException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            AccountDeactivatedException or UnauthorizedFriendshipActionException or UnauthorizedCommunityActionException =>
                (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            EmailAlreadyExistsException or UsernameAlreadyTakenException =>
                (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            DomainException =>
                (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
            cancellationToken);

        return true;
    }
}
