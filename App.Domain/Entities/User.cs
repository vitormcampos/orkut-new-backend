using System.Text.RegularExpressions;

namespace App.Domain.Entities;

public class User
{
    private static readonly Regex UsernameRegex = new(@"^[a-z0-9_]+$", RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? ProfilePicture { get; private set; }
    public string? Bio { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public RelationshipStatus? RelationshipStatus { get; private set; }
    public string[]? MusicInterests { get; private set; }
    public string[]? MovieInterests { get; private set; }
    public string[]? BookInterests { get; private set; }
    public string[]? Hobbies { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetExpiry { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private User() { }

    public User(string name, string email, string username, string passwordHash)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;

        SetName(name);
        SetEmail(email);
        SetUsername(username);
        SetPasswordHash(passwordHash);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
    }

    public void SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));

        if (username.Length < 3 || username.Length > 50)
            throw new ArgumentException("Username must be between 3 and 50 characters.", nameof(username));

        if (!UsernameRegex.IsMatch(username))
            throw new ArgumentException("Username can only contain lowercase letters, numbers and underscores.", nameof(username));

        Username = username;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void SetProfilePicture(string? url)
    {
        ProfilePicture = url;
    }

    public void SetBio(string? bio)
    {
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    }

    public void SetBirthDate(DateOnly? birthDate)
    {
        if (birthDate.HasValue && birthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Birth date cannot be in the future.", nameof(birthDate));
        BirthDate = birthDate;
    }

    public void SetLocation(string? city, string? state)
    {
        City = Normalize(city);
        State = Normalize(state);
    }

    public void SetRelationshipStatus(RelationshipStatus? status) => RelationshipStatus = status;

    public void SetInterests(string[]? music, string[]? movie, string[]? book, string[]? hobbies)
    {
        MusicInterests = NormalizeList(music);
        MovieInterests = NormalizeList(movie);
        BookInterests = NormalizeList(book);
        Hobbies = NormalizeList(hobbies);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string[]? NormalizeList(string[]? values)
    {
        if (values is null) return null;
        var result = values.Select(v => v?.Trim())
            .OfType<string>()
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return result.Length == 0 ? null : result;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
    }

    public void SetLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Reset token cannot be empty.", nameof(token));

        PasswordResetToken = token;
        PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetExpiry = null;
    }

    public bool IsPasswordResetTokenValid()
    {
        return !string.IsNullOrWhiteSpace(PasswordResetToken)
               && PasswordResetExpiry.HasValue
               && PasswordResetExpiry.Value > DateTime.UtcNow;
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("Email must contain '@' symbol.", nameof(email));

        Email = email;
    }
}
