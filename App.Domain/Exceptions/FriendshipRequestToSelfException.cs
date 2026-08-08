namespace App.Domain.Exceptions;

public class FriendshipRequestToSelfException : DomainException
{
    public FriendshipRequestToSelfException()
        : base("Cannot send a friendship request to yourself.") { }
}
