namespace App.Domain.Exceptions;

public sealed class ScrapNotFoundException : DomainException
{
    public ScrapNotFoundException(Guid id)
        : base($"Scrap '{id}' was not found.") { }
}
