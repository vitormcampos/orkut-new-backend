namespace App.Domain.Entities;

public class CommunityMember
{
    public Guid Id { get; private set; }
    public Guid CommunityId { get; private set; }
    public Guid UserId { get; private set; }
    public MembershipRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public Community Community { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private CommunityMember() { }

    public CommunityMember(Guid communityId, Guid userId, MembershipRole role)
    {
        if (communityId == Guid.Empty)
            throw new ArgumentException("CommunityId cannot be empty.", nameof(communityId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        CommunityId = communityId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }
}
