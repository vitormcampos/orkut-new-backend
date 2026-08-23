namespace App.Domain.Entities;

public class Community
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Photo { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User Owner { get; private set; } = null!;
    public ICollection<CommunityMember> Members { get; private set; } = new List<CommunityMember>();

    private Community() { }

    public Community(Guid ownerId, string name, string? description)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;

        SetName(name);
        SetDescription(description);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name.Trim();
    }

    public void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetPhoto(string? url)
    {
        Photo = url;
    }

    public void Update(string name, string? description)
    {
        SetName(name);
        SetDescription(description);
        UpdatedAt = DateTime.UtcNow;
    }
}
