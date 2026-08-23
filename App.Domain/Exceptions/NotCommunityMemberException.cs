namespace App.Domain.Exceptions;

public class NotCommunityMemberException : DomainException
{
    public NotCommunityMemberException()
        : base("User is not a member of this community.") { }
}
