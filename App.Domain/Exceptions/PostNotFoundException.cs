namespace App.Domain.Exceptions;

public class PostNotFoundException : DomainException
{
    public PostNotFoundException(Guid postId)
        : base($"Post with Id '{postId}' was not found.") { }
}
