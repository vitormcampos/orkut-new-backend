namespace App.Domain.Entities;

public class Post
{
    public const int MaxContentLength = 5000;

    public Guid Id { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? CommunityId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public User Author { get; private set; } = null!;
    public Community? Community { get; private set; }

    private Post() { }

    public Post(Guid authorId, Guid? communityId, string content)
    {
        if (authorId == Guid.Empty)
            throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));

        if (communityId == Guid.Empty)
            throw new ArgumentException("CommunityId cannot be empty.", nameof(communityId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Post content cannot be empty.", nameof(content));

        var normalizedContent = content.Trim();
        if (normalizedContent.Length > MaxContentLength)
            throw new ArgumentException($"Post content cannot exceed {MaxContentLength} characters.", nameof(content));

        Id = Guid.NewGuid();
        AuthorId = authorId;
        CommunityId = communityId;
        Content = normalizedContent;
        CreatedAt = DateTime.UtcNow;
    }
}
