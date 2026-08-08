namespace App.Domain.Exceptions;

public class FriendshipNotFoundException : DomainException
{
    public FriendshipNotFoundException(Guid friendshipId)
        : base($"Friendship with Id '{friendshipId}' was not found.") { }
}
