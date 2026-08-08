namespace App.Domain.Entities;

public class Friendship
{
    public Guid Id { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public FriendshipStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public User Requester { get; private set; } = null!;
    public User Addressee { get; private set; } = null!;

    private Friendship() { }

    public Friendship(Guid requesterId, Guid addresseeId)
    {
        if (requesterId == addresseeId)
            throw new ArgumentException("Cannot send a friendship request to yourself.");

        Id = Guid.NewGuid();
        RequesterId = requesterId;
        AddresseeId = addresseeId;
        Status = FriendshipStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public void Accept()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be accepted.");

        Status = FriendshipStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        Status = FriendshipStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
    }

    public void Resend()
    {
        if (Status != FriendshipStatus.Rejected)
            throw new InvalidOperationException("Only rejected requests can be resent.");

        Status = FriendshipStatus.Pending;
        RequestedAt = DateTime.UtcNow;
        RespondedAt = null;
    }
}
