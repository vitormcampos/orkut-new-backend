using App.Application.DTOs;

namespace App.Application.Interfaces;

public interface IFriendshipService
{
    Task<FriendshipDto> SendRequestAsync(Guid requesterId, string addresseeUsername, CancellationToken ct = default);
    Task<FriendshipDto> AcceptRequestAsync(Guid friendshipId, Guid currentUserId, CancellationToken ct = default);
    Task<FriendshipDto> RejectRequestAsync(Guid friendshipId, Guid currentUserId, CancellationToken ct = default);
    Task RemoveFriendshipAsync(Guid friendshipId, Guid currentUserId, CancellationToken ct = default);
    Task<IEnumerable<FriendshipDto>> GetFriendsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<FriendshipDto>> GetPendingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<FriendshipDto>> GetSentRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetFriendsInCommonAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
}
