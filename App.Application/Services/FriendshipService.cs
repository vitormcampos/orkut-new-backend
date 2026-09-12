using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Services;

public class FriendshipService : IFriendshipService
{
    private readonly DbContext _context;

    public FriendshipService(DbContext context)
    {
        _context = context;
    }

    public async Task<FriendshipDto> SendRequestAsync(
        Guid requesterId,
        string addresseeUsername,
        CancellationToken ct = default
    )
    {
        var addressee = await _context
            .Set<User>()
            .FirstOrDefaultAsync(u => u.Username == addresseeUsername && u.IsActive, ct);

        if (addressee is null)
            throw new UserNotFoundException(addresseeUsername);

        var addresseeId = addressee.Id;

        if (requesterId == addresseeId)
            throw new FriendshipRequestToSelfException();

        var alreadyFriends = await _context
            .Set<Friendship>()
            .AnyAsync(
                f =>
                    (
                        f.RequesterId == requesterId
                        && f.AddresseeId == addresseeId
                        && f.Status == FriendshipStatus.Accepted
                    )
                    || (
                        f.RequesterId == addresseeId
                        && f.AddresseeId == requesterId
                        && f.Status == FriendshipStatus.Accepted
                    ),
                ct
            );

        if (alreadyFriends)
            throw new AlreadyFriendsException();

        var existing = await _context
            .Set<Friendship>()
            .FirstOrDefaultAsync(
                f => f.RequesterId == requesterId && f.AddresseeId == addresseeId,
                ct
            );

        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Pending)
            {
                await _context.Entry(existing).Reference(f => f.Requester).LoadAsync(ct);
                await _context.Entry(existing).Reference(f => f.Addressee).LoadAsync(ct);
                return MapToDto(existing);
            }

            if (existing.Status == FriendshipStatus.Rejected)
            {
                existing.Resend();
                await _context.SaveChangesAsync(ct);
                await _context.Entry(existing).Reference(f => f.Requester).LoadAsync(ct);
                await _context.Entry(existing).Reference(f => f.Addressee).LoadAsync(ct);
                return MapToDto(existing);
            }
        }

        var friendship = new Friendship(requesterId, addresseeId);
        _context.Set<Friendship>().Add(friendship);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(friendship).Reference(f => f.Requester).LoadAsync(ct);
        await _context.Entry(friendship).Reference(f => f.Addressee).LoadAsync(ct);

        return MapToDto(friendship);
    }

    public async Task<FriendshipDto> AcceptRequestAsync(
        Guid friendshipId,
        Guid currentUserId,
        CancellationToken ct = default
    )
    {
        var friendship = await _context
            .Set<Friendship>()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friendship is null)
            throw new FriendshipNotFoundException(friendshipId);

        if (friendship.AddresseeId != currentUserId)
            throw new UnauthorizedFriendshipActionException();

        friendship.Accept();
        await _context.SaveChangesAsync(ct);

        return MapToDto(friendship);
    }

    public async Task<FriendshipDto> RejectRequestAsync(
        Guid friendshipId,
        Guid currentUserId,
        CancellationToken ct = default
    )
    {
        var friendship = await _context
            .Set<Friendship>()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friendship is null)
            throw new FriendshipNotFoundException(friendshipId);

        if (friendship.AddresseeId != currentUserId)
            throw new UnauthorizedFriendshipActionException();

        friendship.Reject();
        await _context.SaveChangesAsync(ct);

        return MapToDto(friendship);
    }

    public async Task RemoveFriendshipAsync(
        Guid friendshipId,
        Guid currentUserId,
        CancellationToken ct = default
    )
    {
        var friendship = await _context.Set<Friendship>().FindAsync([friendshipId], ct);

        if (friendship is null)
            throw new FriendshipNotFoundException(friendshipId);

        if (friendship.RequesterId != currentUserId && friendship.AddresseeId != currentUserId)
            throw new NotFriendshipParticipantException();

        _context.Set<Friendship>().Remove(friendship);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<FriendshipDto>> GetFriendsAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var friendships = await _context
            .Set<Friendship>()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f =>
                (f.RequesterId == userId || f.AddresseeId == userId)
                && f.Status == FriendshipStatus.Accepted
            )
            .OrderBy(f => f.RespondedAt)
            .ToListAsync(ct);

        return friendships.Select(MapToDto);
    }

    public async Task<IEnumerable<FriendshipDto>> GetPendingRequestsAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var friendships = await _context
            .Set<Friendship>()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .OrderBy(f => f.RequestedAt)
            .ToListAsync(ct);

        return friendships.Select(MapToDto);
    }

    public async Task<IEnumerable<FriendshipDto>> GetSentRequestsAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var friendships = await _context
            .Set<Friendship>()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
            .OrderBy(f => f.RequestedAt)
            .ToListAsync(ct);

        return friendships.Select(MapToDto);
    }

    public async Task<IEnumerable<PublicUserDto>> GetFriendsInCommonAsync(
        Guid userId,
        Guid otherUserId,
        CancellationToken ct = default
    )
    {
        var userFriendIds = await _context
            .Set<Friendship>()
            .Where(f =>
                (f.RequesterId == userId || f.AddresseeId == userId)
                && f.Status == FriendshipStatus.Accepted
            )
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToListAsync(ct);

        var otherFriendIds = await _context
            .Set<Friendship>()
            .Where(f =>
                (f.RequesterId == otherUserId || f.AddresseeId == otherUserId)
                && f.Status == FriendshipStatus.Accepted
            )
            .Select(f => f.RequesterId == otherUserId ? f.AddresseeId : f.RequesterId)
            .ToListAsync(ct);

        var commonIds = userFriendIds.Intersect(otherFriendIds).ToList();

        if (commonIds.Count == 0)
            return Enumerable.Empty<PublicUserDto>();

        var users = await _context
            .Set<User>()
            .Where(u => commonIds.Contains(u.Id))
            .Select(u => new PublicUserDto(
                u.Id,
                u.Name,
                u.Username,
                u.ProfilePicture,
                u.Bio,
                u.CreatedAt
            ))
            .ToListAsync(ct);

        return users;
    }

    public async Task<int> GetFriendCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context
            .Set<Friendship>()
            .CountAsync(
                f =>
                    (f.RequesterId == userId || f.AddresseeId == userId)
                    && f.Status == FriendshipStatus.Accepted,
                ct
            );
    }

    private static PublicUserDto MapToSummaryDto(User user)
    {
        return new PublicUserDto(
            user.Id,
            user.Name,
            user.Username,
            user.ProfilePicture,
            user.Bio,
            user.CreatedAt
        );
    }

    private static FriendshipDto MapToDto(Friendship friendship)
    {
        return new FriendshipDto(
            friendship.Id,
            friendship.RequesterId,
            friendship.Requester.Name,
            friendship.Requester.Username,
            friendship.Requester.ProfilePicture,
            friendship.AddresseeId,
            friendship.Addressee.Name,
            friendship.Addressee.Username,
            friendship.Addressee.ProfilePicture,
            friendship.Status.ToString(),
            friendship.RequestedAt,
            friendship.RespondedAt
        );
    }
}
