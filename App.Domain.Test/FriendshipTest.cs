using App.Domain.Entities;

namespace App.Domain.Test;

public class FriendshipTest
{
    [Fact]
    public void Constructor_ShouldCreateFriendship_WhenDataIsValid()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var addresseeId = Guid.NewGuid();

        // Act
        var friendship = new Friendship(requesterId, addresseeId);

        // Assert
        Assert.NotEqual(Guid.Empty, friendship.Id);
        Assert.Equal(requesterId, friendship.RequesterId);
        Assert.Equal(addresseeId, friendship.AddresseeId);
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        Assert.True(friendship.RequestedAt <= DateTime.UtcNow);
        Assert.Null(friendship.RespondedAt);
    }

    [Fact]
    public void Constructor_ShouldGenerateDifferentFriendships_OnEachCall()
    {
        // Arrange & Act
        var f1 = new Friendship(Guid.NewGuid(), Guid.NewGuid());
        var f2 = new Friendship(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.NotEqual(f1.Id, f2.Id);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenRequesterIdEqualsAddresseeId()
    {
        // Arrange
        var sameId = Guid.NewGuid();

        // Act
        var act = () => new Friendship(sameId, sameId);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("yourself", exception.Message);
    }

    [Fact]
    public void Accept_ShouldSetStatusAcceptedAndRespondedAt()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());

        // Act
        friendship.Accept();

        // Assert
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        Assert.NotNull(friendship.RespondedAt);
        Assert.True(friendship.RespondedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenStatusIsNotPending()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());
        friendship.Accept();

        // Act
        var act = () => friendship.Accept();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("pending", exception.Message);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenStatusIsRejected()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());
        friendship.Reject();

        // Act
        var act = () => friendship.Accept();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Reject_ShouldSetStatusRejectedAndRespondedAt()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());

        // Act
        friendship.Reject();

        // Assert
        Assert.Equal(FriendshipStatus.Rejected, friendship.Status);
        Assert.NotNull(friendship.RespondedAt);
        Assert.True(friendship.RespondedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Reject_ShouldThrow_WhenStatusIsNotPending()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());
        friendship.Accept();

        // Act
        var act = () => friendship.Reject();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Resend_ShouldResetToPending_WhenStatusIsRejected()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());
        friendship.Reject();
        var originalRequestedAt = friendship.RequestedAt;

        // Act
        friendship.Resend();

        // Assert
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        Assert.Null(friendship.RespondedAt);
        Assert.True(friendship.RequestedAt >= originalRequestedAt);
    }

    [Fact]
    public void Resend_ShouldThrow_WhenStatusIsPending()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var act = () => friendship.Resend();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Resend_ShouldThrow_WhenStatusIsAccepted()
    {
        // Arrange
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid());
        friendship.Accept();

        // Act
        var act = () => friendship.Resend();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}
