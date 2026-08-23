using App.Domain.Entities;

namespace App.Domain.Test;

public class CommunityMemberTest
{
    [Fact]
    public void Constructor_ShouldCreateCommunityMember_WhenDataIsValid()
    {
        // Arrange
        var communityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var member = new CommunityMember(communityId, userId, MembershipRole.Member);

        // Assert
        Assert.NotEqual(Guid.Empty, member.Id);
        Assert.Equal(communityId, member.CommunityId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(MembershipRole.Member, member.Role);
        Assert.True(member.JoinedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldGenerateDifferentMembers_OnEachCall()
    {
        // Arrange & Act
        var m1 = new CommunityMember(Guid.NewGuid(), Guid.NewGuid(), MembershipRole.Owner);
        var m2 = new CommunityMember(Guid.NewGuid(), Guid.NewGuid(), MembershipRole.Member);

        // Assert
        Assert.NotEqual(m1.Id, m2.Id);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCommunityIdIsEmpty()
    {
        // Arrange
        var emptyCommunityId = Guid.Empty;
        var userId = Guid.NewGuid();

        // Act
        var act = () => new CommunityMember(emptyCommunityId, userId, MembershipRole.Member);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("CommunityId", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUserIdIsEmpty()
    {
        // Arrange
        var communityId = Guid.NewGuid();
        var emptyUserId = Guid.Empty;

        // Act
        var act = () => new CommunityMember(communityId, emptyUserId, MembershipRole.Member);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("UserId", exception.Message);
    }

    [Theory]
    [InlineData(MembershipRole.Owner)]
    [InlineData(MembershipRole.Member)]
    public void Constructor_ShouldSetRole_ForAllRoles(MembershipRole role)
    {
        // Arrange
        var communityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var member = new CommunityMember(communityId, userId, role);

        // Assert
        Assert.Equal(role, member.Role);
    }
}
