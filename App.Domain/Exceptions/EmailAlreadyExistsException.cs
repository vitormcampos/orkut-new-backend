namespace App.Domain.Exceptions;

public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base($"A user with email '{email}' already exists.") { }
}
