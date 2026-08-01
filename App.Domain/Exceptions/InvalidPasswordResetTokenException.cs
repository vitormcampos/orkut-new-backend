namespace App.Domain.Exceptions;

public class InvalidPasswordResetTokenException : DomainException
{
    public InvalidPasswordResetTokenException()
        : base("The password reset token is invalid or has expired.") { }
}
