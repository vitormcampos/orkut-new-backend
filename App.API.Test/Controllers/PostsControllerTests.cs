using System.Net;
using System.Net.Http.Json;
using App.API.Test.Shared;
using App.Application.DTOs;

namespace App.API.Test.Controllers;

public sealed class PostsControllerTests : ApiTestBase
{
    public PostsControllerTests(App.API.Test.Infrastructure.ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_ShouldCreatePersonalPost_AndAllowAnonymousRead()
    {
        // Arrange
        var author = await RegisterUserAsync("post_author");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
        {
            Content = JsonContent.Create(new CreatePostRequest(null, "Post público"))
        };
        Authenticate(request, author.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await response.Content.ReadFromJsonAsync<PostDto>();
        Assert.NotNull(post);
        Assert.Null(post!.CommunityId);
        var publicResponse = await Client.GetAsync($"/api/v1/posts/users/{author.Id}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var page = await publicResponse.Content.ReadFromJsonAsync<PostPageDto>();
        Assert.Equal("Post público", Assert.Single(page!.Items).Content);
    }

    [Fact]
    public async Task Create_ShouldRejectNonMemberPostingToCommunity()
    {
        // Arrange
        var owner = await RegisterUserAsync("post_owner");
        var outsider = await RegisterUserAsync("post_outsider");
        using var communityRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/communities")
        {
            Content = JsonContent.Create(new CreateCommunityRequest("Post community", "Description"))
        };
        Authenticate(communityRequest, owner.Id);
        var communityResponse = await Client.SendAsync(communityRequest);
        communityResponse.EnsureSuccessStatusCode();
        var community = await communityResponse.Content.ReadFromJsonAsync<CommunityDto>();

        using var postRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
        {
            Content = JsonContent.Create(new CreatePostRequest(community!.Id, "Not a member"))
        };
        Authenticate(postRequest, outsider.Id);

        // Act
        var response = await Client.SendAsync(postRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldAllowCommunityMemberToPost()
    {
        // Arrange
        var owner = await RegisterUserAsync("post_owner_member");
        var member = await RegisterUserAsync("post_member");
        using var communityRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/communities")
        {
            Content = JsonContent.Create(new CreateCommunityRequest("Members only post", "Description"))
        };
        Authenticate(communityRequest, owner.Id);
        var communityResponse = await Client.SendAsync(communityRequest);
        communityResponse.EnsureSuccessStatusCode();
        var community = await communityResponse.Content.ReadFromJsonAsync<CommunityDto>();

        using var joinRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/communities/{community!.Id}/join");
        Authenticate(joinRequest, member.Id);
        (await Client.SendAsync(joinRequest)).EnsureSuccessStatusCode();

        using var postRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts")
        {
            Content = JsonContent.Create(new CreatePostRequest(community.Id, "Post de membro"))
        };
        Authenticate(postRequest, member.Id);

        // Act
        var response = await Client.SendAsync(postRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await response.Content.ReadFromJsonAsync<PostDto>();
        Assert.Equal(community.Id, post!.CommunityId);
    }
}
