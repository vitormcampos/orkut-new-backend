namespace App.Domain.Exceptions;

public class AccountDeactivatedException : DomainException
{
    public AccountDeactivatedException()
        : base("This account has been deactivated.") { }
}
