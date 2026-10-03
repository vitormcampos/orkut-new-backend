namespace App.Domain.Exceptions;

public class UnauthorizedPostActionException : DomainException
{
    public UnauthorizedPostActionException()
        : base("The user is not authorized to perform this action on the post.") { }
}
