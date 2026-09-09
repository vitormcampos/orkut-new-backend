using System.Net;
using System.Net.Http.Json;
using App.API.Test.Infrastructure;
using App.API.Test.Shared;
using App.Application.DTOs;

namespace App.API.Test.Controllers;

public sealed class ScrapsControllerTests : ApiTestBase
{
    public ScrapsControllerTests(ApiFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Create_ShouldRequireAuthentication()
    {
        // Arrange
        var recipient = await RegisterUserAsync("unauth_recipient");
        var request = new CreateScrapRequest(recipient.Id, "Oi!");

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/scraps", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedScrap()
    {
        // Arrange
        var author = await RegisterUserAsync("create_author");
        var recipient = await RegisterUserAsync("create_recipient");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/scraps")
        {
            Content = JsonContent.Create(new CreateScrapRequest(recipient.Id, "Fala, sumido!"))
        };
        Authenticate(request, author.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var scrap = await response.Content.ReadFromJsonAsync<ScrapDto>();
        Assert.NotNull(scrap);
        Assert.Equal(author.Id, scrap!.AuthorId);
        Assert.Equal(recipient.Id, scrap.RecipientId);
        Assert.Equal("Fala, sumido!", scrap.Content);
        Assert.Equal("Public", scrap.Visibility);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldReturnPublicScraps()
    {
        // Arrange
        var author = await RegisterUserAsync("public_author");
        var recipient = await RegisterUserAsync("public_recipient");
        await CreateScrapAsync(author, recipient, "Recado público");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/scraps/profile/{recipient.Id}");
        Authenticate(request, author.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ScrapPageDto>();
        var scrap = Assert.Single(page!.Items);
        Assert.Equal("Recado público", scrap.Content);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldHidePrivateScrapFromThirdParty()
    {
        // Arrange
        var author = await RegisterUserAsync("private_author");
        var recipient = await RegisterUserAsync("private_recipient");
        var thirdParty = await RegisterUserAsync("private_third_party");
        await CreateScrapAsync(author, recipient, "Recado privado", "Private");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/scraps/profile/{recipient.Id}");
        Authenticate(request, thirdParty.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ScrapPageDto>();
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldShowPrivateScrapToAuthorAndRecipient()
    {
        // Arrange
        var author = await RegisterUserAsync("visible_author");
        var recipient = await RegisterUserAsync("visible_recipient");
        var scrap = await CreateScrapAsync(author, recipient, "Só nós", "Private");

        using var authorRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/scraps/profile/{recipient.Id}");
        Authenticate(authorRequest, author.Id);
        using var recipientRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/scraps/profile/{recipient.Id}");
        Authenticate(recipientRequest, recipient.Id);

        // Act
        var authorResponse = await Client.SendAsync(authorRequest);
        var recipientResponse = await Client.SendAsync(recipientRequest);
        var authorPage = await authorResponse.Content.ReadFromJsonAsync<ScrapPageDto>();
        var recipientPage = await recipientResponse.Content.ReadFromJsonAsync<ScrapPageDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, authorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, recipientResponse.StatusCode);
        Assert.Equal(scrap.Id, Assert.Single(authorPage!.Items).Id);
        Assert.Equal(scrap.Id, Assert.Single(recipientPage!.Items).Id);
    }

    [Fact]
    public async Task Delete_ShouldAllowOnlyTheAuthor()
    {
        // Arrange
        var author = await RegisterUserAsync("delete_author");
        var recipient = await RegisterUserAsync("delete_recipient");
        var scrap = await CreateScrapAsync(author, recipient, "Apague-me");

        using var unauthorizedRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/scraps/{scrap.Id}");
        Authenticate(unauthorizedRequest, recipient.Id);

        // Act
        var unauthorizedResponse = await Client.SendAsync(unauthorizedRequest);

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/scraps/{scrap.Id}");
        Authenticate(deleteRequest, author.Id);
        var deleteResponse = await Client.SendAsync(deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForUnknownScrap()
    {
        // Arrange
        var author = await RegisterUserAsync("missing_scrap");
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/scraps/{Guid.NewGuid()}");
        Authenticate(request, author.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequestWhenRecipientIsAuthor()
    {
        // Arrange
        var user = await RegisterUserAsync("self_scrap");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/scraps")
        {
            Content = JsonContent.Create(new CreateScrapRequest(user.Id, "Para mim"))
        };
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFoundForUnknownRecipient()
    {
        // Arrange
        var author = await RegisterUserAsync("missing_scrap_recipient");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/scraps")
        {
            Content = JsonContent.Create(
                new CreateScrapRequest(Guid.NewGuid(), "Destinatário ausente"))
        };
        Authenticate(request, author.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ScrapDto> CreateScrapAsync(
        UserDto author,
        UserDto recipient,
        string content,
        string? visibility = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/scraps")
        {
            Content = JsonContent.Create(new CreateScrapRequest(recipient.Id, content, visibility))
        };
        Authenticate(request, author.Id);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScrapDto>())!;
    }
}
