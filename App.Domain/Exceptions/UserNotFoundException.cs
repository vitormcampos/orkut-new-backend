namespace App.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string username)
        : base($"User with username '{username}' was not found.") { }
}
