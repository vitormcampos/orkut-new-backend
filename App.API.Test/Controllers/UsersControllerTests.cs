using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.API.Test.Infrastructure;
using App.API.Test.Shared;
using App.Application.DTOs;

namespace App.API.Test.Controllers;

public sealed class UsersControllerTests : ApiTestBase
{
    public UsersControllerTests(ApiFactory factory)
        : base(factory) { }

    [Fact]
    public async Task GetProfile_ShouldRequireAuthentication()
    {
        var response = await Client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ShouldReturnCurrentUser()
    {
        // Arrange
        var user = await RegisterUserAsync("profile");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnUpdatedProfile()
    {
        // Arrange
        var user = await RegisterUserAsync("update");
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me")
        {
            Content = JsonContent.Create(
                new UpdateProfileRequest(
                    "Updated Name",
                    null,
                    "Updated bio",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                )
            ),
        };
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateAccount_ShouldReturnNoContent()
    {
        // Arrange
        var user = await RegisterUserAsync("deactivate");
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me");
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_ShouldUpdateProfilePictureForValidFile()
    {
        // Arrange
        var user = await RegisterUserAsync("valid_photo");
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "avatar.png");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/photo")
        {
            Content = content,
        };
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Contains("storage.test", result!.ProfilePicture);
    }

    [Fact]
    public async Task UploadPhoto_ShouldReturnInternalServerErrorWhenStorageFails()
    {
        // Arrange
        var user = await RegisterUserAsync("storage_failure");
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent([9, 8, 7]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "avatar.png");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/photo")
        {
            Content = content,
        };
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Simulated storage upload failure", body);
    }

    [Fact]
    public async Task UploadPhoto_ShouldRejectMissingFile()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/photo");
        request.Headers.Add("X-Test-Auth", "true");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_ShouldRejectUnsupportedContentType()
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "avatar.pdf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/photo")
        {
            Content = content,
        };
        request.Headers.Add("X-Test-Auth", "true");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_ShouldRejectFilesLargerThanFiveMegabytes()
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(new byte[5 * 1024 * 1024 + 1]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "avatar.png");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/photo")
        {
            Content = content,
        };
        request.Headers.Add("X-Test-Auth", "true");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
