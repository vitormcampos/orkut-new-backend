namespace App.Domain.Entities;

public class Scrap
{
    public const int MaxContentLength = 500;

    public Guid Id { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid RecipientId { get; private set; }
    public string Content { get; private set; } = null!;
    public ScrapVisibility Visibility { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User Author { get; private set; } = null!;
    public User Recipient { get; private set; } = null!;

    private Scrap() { }

    public Scrap(Guid authorId, Guid recipientId, string content, ScrapVisibility visibility = ScrapVisibility.Public)
    {
        if (authorId == recipientId)
            throw new ArgumentException("Cannot leave a scrap for yourself.");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Scrap content cannot be empty.", nameof(content));

        var normalizedContent = content.Trim();
        if (normalizedContent.Length > MaxContentLength)
            throw new ArgumentException($"Scrap content cannot exceed {MaxContentLength} characters.", nameof(content));

        Id = Guid.NewGuid();
        AuthorId = authorId;
        RecipientId = recipientId;
        Content = normalizedContent;
        Visibility = visibility;
        CreatedAt = DateTime.UtcNow;
    }
}
