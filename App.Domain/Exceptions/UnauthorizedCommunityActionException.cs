namespace App.Domain.Exceptions;

public class UnauthorizedCommunityActionException : DomainException
{
    public UnauthorizedCommunityActionException()
        : base("Only the community owner can perform this action.") { }
}
