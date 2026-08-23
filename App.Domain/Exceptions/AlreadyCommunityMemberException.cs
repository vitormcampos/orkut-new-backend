namespace App.Domain.Exceptions;

public class AlreadyCommunityMemberException : DomainException
{
    public AlreadyCommunityMemberException()
        : base("User is already a member of this community.") { }
}
