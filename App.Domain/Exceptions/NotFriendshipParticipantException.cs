namespace App.Domain.Exceptions;

public class NotFriendshipParticipantException : DomainException
{
    public NotFriendshipParticipantException()
        : base("Only participants can remove a friendship.") { }
}
