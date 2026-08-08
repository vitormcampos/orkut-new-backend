namespace App.Domain.Exceptions;

public class FriendshipAlreadyExistsException : DomainException
{
    public FriendshipAlreadyExistsException()
        : base("A friendship relationship already exists between these users.") { }
}
