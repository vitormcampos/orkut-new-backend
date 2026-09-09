using System.Net;
using System.Net.Http.Json;
using App.API.Test.Infrastructure;
using App.API.Test.Shared;
using App.Application.DTOs;

namespace App.API.Test.Controllers;

public sealed class FriendshipsControllerTests : ApiTestBase
{
    public FriendshipsControllerTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task SendRequest_ShouldCreateFriendship()
    {
        // Arrange
        var requester = await RegisterUserAsync("requester");
        var addressee = await RegisterUserAsync("addressee");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/friendships/request")
        {
            Content = JsonContent.Create(new SendFriendshipRequest(addressee.Username))
        };
        Authenticate(request, requester.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var friendship = await response.Content.ReadFromJsonAsync<FriendshipDto>();
        Assert.NotNull(friendship);
        Assert.Equal(requester.Id, friendship!.RequesterId);
        Assert.Equal(addressee.Id, friendship.AddresseeId);
    }

    [Fact]
    public async Task AcceptListAndRemove_ShouldManageFriendship()
    {
        // Arrange
        var requester = await RegisterUserAsync("flow_requester");
        var addressee = await RegisterUserAsync("flow_addressee");
        using var sendRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/friendships/request")
        {
            Content = JsonContent.Create(new SendFriendshipRequest(addressee.Username))
        };
        Authenticate(sendRequest, requester.Id);
        var sendResponse = await Client.SendAsync(sendRequest);
        var friendship = await sendResponse.Content.ReadFromJsonAsync<FriendshipDto>();

        // Act
        using var acceptRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/friendships/{friendship!.Id}/accept");
        Authenticate(acceptRequest, addressee.Id);
        var acceptResponse = await Client.SendAsync(acceptRequest);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/friendships");
        Authenticate(listRequest, requester.Id);
        var listResponse = await Client.SendAsync(listRequest);
        var friends = await listResponse.Content.ReadFromJsonAsync<List<FriendshipDto>>();

        using var removeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/friendships/{friendship.Id}");
        Authenticate(removeRequest, requester.Id);
        var removeResponse = await Client.SendAsync(removeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Single(friends!);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }

    [Fact]
    public async Task SendRequest_ShouldReturnNotFoundForUnknownUser()
    {
        // Arrange
        var requester = await RegisterUserAsync("missing_addressee");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/friendships/request")
        {
            Content = JsonContent.Create(new SendFriendshipRequest("unknown_user"))
        };
        Authenticate(request, requester.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendRequest_ShouldReturnBadRequestWhenRequestingSelf()
    {
        // Arrange
        var user = await RegisterUserAsync("self_request");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/friendships/request")
        {
            Content = JsonContent.Create(new SendFriendshipRequest(user.Username))
        };
        Authenticate(request, user.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcceptRequest_ShouldReturnForbiddenForNonAddressee()
    {
        // Arrange
        var requester = await RegisterUserAsync("unauthorized_requester");
        var addressee = await RegisterUserAsync("unauthorized_addressee");
        var otherUser = await RegisterUserAsync("unauthorized_other");
        var friendship = await SendRequestAsync(requester, addressee);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/friendships/{friendship.Id}/accept");
        Authenticate(request, otherUser.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RejectRequest_ShouldReturnRejectedFriendship()
    {
        // Arrange
        var requester = await RegisterUserAsync("reject_requester");
        var addressee = await RegisterUserAsync("reject_addressee");
        var friendship = await SendRequestAsync(requester, addressee);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/friendships/{friendship.Id}/reject");
        Authenticate(request, addressee.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FriendshipDto>();
        Assert.Equal("Rejected", result!.Status);
    }

    [Fact]
    public async Task GetPendingRequests_ShouldReturnReceivedPendingRequests()
    {
        // Arrange
        var target = await RegisterUserAsync("pending_target");
        var first = await RegisterUserAsync("pending_first");
        var second = await RegisterUserAsync("pending_second");
        await SendRequestAsync(first, target);
        await SendRequestAsync(second, target);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/friendships/pending");
        Authenticate(request, target.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FriendshipDto>>();
        Assert.Equal(2, result!.Count);
        Assert.All(result, item => Assert.Equal(target.Id, item.AddresseeId));
    }

    [Fact]
    public async Task GetSentRequests_ShouldReturnPendingRequestsSentByUser()
    {
        // Arrange
        var requester = await RegisterUserAsync("sent_requester");
        var first = await RegisterUserAsync("sent_first");
        var second = await RegisterUserAsync("sent_second");
        await SendRequestAsync(requester, first);
        await SendRequestAsync(requester, second);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/friendships/sent");
        Authenticate(request, requester.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FriendshipDto>>();
        Assert.Equal(2, result!.Count);
        Assert.All(result, item => Assert.Equal(requester.Id, item.RequesterId));
    }

    [Fact]
    public async Task GetFriendsInCommon_ShouldReturnMutualFriendsWithoutEmail()
    {
        // Arrange
        var alice = await RegisterUserAsync("common_alice");
        var bob = await RegisterUserAsync("common_bob");
        var charlie = await RegisterUserAsync("common_charlie");
        await CreateAcceptedFriendshipAsync(alice, charlie);
        await CreateAcceptedFriendshipAsync(bob, charlie);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/friendships/in-common/{bob.Id}");
        Authenticate(request, alice.Id);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<PublicUserDto>>();
        var mutual = Assert.Single(result!);
        Assert.Equal(charlie.Id, mutual.Id);
        Assert.DoesNotContain("email", (await response.Content.ReadAsStringAsync()).ToLowerInvariant());
    }

    private async Task<FriendshipDto> SendRequestAsync(UserDto requester, UserDto addressee)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/friendships/request")
        {
            Content = JsonContent.Create(new SendFriendshipRequest(addressee.Username))
        };
        Authenticate(request, requester.Id);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FriendshipDto>())!;
    }

    private async Task CreateAcceptedFriendshipAsync(UserDto requester, UserDto addressee)
    {
        var friendship = await SendRequestAsync(requester, addressee);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/friendships/{friendship.Id}/accept");
        Authenticate(request, addressee.Id);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetFriends_ShouldRequireAuthentication()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/friendships");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
