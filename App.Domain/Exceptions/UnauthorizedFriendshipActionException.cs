namespace App.Domain.Exceptions;

public class UnauthorizedFriendshipActionException : DomainException
{
    public UnauthorizedFriendshipActionException()
        : base("Only the addressee can accept or reject a friendship request.") { }
}
