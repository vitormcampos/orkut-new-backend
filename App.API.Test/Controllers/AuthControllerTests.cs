using System.Net;
using System.Net.Http.Json;
using App.API.Test.Infrastructure;
using App.API.Test.Shared;
using App.Application.DTOs;

namespace App.API.Test.Controllers;

public sealed class AuthControllerTests : ApiTestBase
{
    public AuthControllerTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_ShouldReturnTokenForRegisteredUser()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login_{suffix}@example.com";
        var password = "Password123!";
        await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CreateUserRequest("Login User", email, $"login_{suffix}", password));

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task Login_ShouldRejectInvalidCredentials()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("missing@example.com", "WrongPassword"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldRejectShortPassword()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CreateUserRequest(
                "Invalid User",
                $"invalid_{Guid.NewGuid():N}@example.com",
                $"invalid_{Guid.NewGuid():N}"[..20],
                "short"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldCreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CreateUserRequest(
                "Integration User",
                $"{suffix}@example.com",
                $"integration_{suffix}",
                "Password123!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
