namespace App.Domain.Exceptions;

public class UsernameAlreadyTakenException : DomainException
{
    public UsernameAlreadyTakenException(string username)
        : base($"The username '{username}' is already taken.") { }
}
