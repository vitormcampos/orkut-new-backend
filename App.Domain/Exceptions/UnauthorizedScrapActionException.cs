namespace App.Domain.Exceptions;

public sealed class UnauthorizedScrapActionException : DomainException
{
    public UnauthorizedScrapActionException()
        : base("Only the scrap author can delete it.") { }
}
