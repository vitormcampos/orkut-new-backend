namespace App.Domain.Exceptions;

public class AlreadyFriendsException : DomainException
{
    public AlreadyFriendsException()
        : base("These users are already friends.") { }
}
