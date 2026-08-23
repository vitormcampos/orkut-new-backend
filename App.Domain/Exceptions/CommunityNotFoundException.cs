namespace App.Domain.Exceptions;

public class CommunityNotFoundException : DomainException
{
    public CommunityNotFoundException(Guid communityId)
        : base($"Community with Id '{communityId}' was not found.") { }
}
