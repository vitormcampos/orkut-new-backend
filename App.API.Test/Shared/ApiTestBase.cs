using System.Net.Http.Json;
using App.Application.DTOs;
using App.API.Test.Infrastructure;

namespace App.API.Test.Shared;

public abstract class ApiTestBase : IClassFixture<ApiFactory>
{
    protected ApiTestBase(ApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }

    protected async Task<UserDto> RegisterUserAsync(string prefix = "user")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CreateUserRequest(
                $"{prefix} user",
                $"{prefix}_{suffix}@example.com",
                $"{prefix}_{suffix}",
                "Password123!"));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    protected static void Authenticate(HttpRequestMessage request, Guid userId)
    {
        request.Headers.Add("X-Test-Auth", "true");
        request.Headers.Add("X-Test-User-Id", userId.ToString());
    }
}
