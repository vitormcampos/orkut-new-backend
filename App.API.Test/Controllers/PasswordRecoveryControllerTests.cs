using System.Net;
using System.Net.Http.Json;
using App.Application.DTOs;
using App.API.Test.Infrastructure;
using App.API.Test.Shared;

namespace App.API.Test.Controllers;

public sealed class PasswordRecoveryControllerTests : ApiTestBase
{
    public PasswordRecoveryControllerTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Forgot_ShouldReturnNoContentForUnknownEmail()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/passwordrecovery/forgot",
            new ForgotPasswordRequest("unknown@example.com"));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Reset_ShouldReturnBadRequestForUnknownToken()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/passwordrecovery/reset",
            new ResetPasswordRequest("unknown-token", "NewPassword123!"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Forgot_ShouldBePublic()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/passwordrecovery/forgot",
            new ForgotPasswordRequest("unknown@example.com"));

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
