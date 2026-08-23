using App.Domain.Entities;
using App.Domain.Test.Fakers;

namespace App.Domain.Test;

public class UserTest
{
    [Fact]
    public void Constructor_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act & Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.NotNull(user.Name);
        Assert.NotNull(user.Email);
        Assert.NotNull(user.Username);
        Assert.NotNull(user.PasswordHash);
        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldGenerateDifferentUsers_OnEachCall()
    {
        // Arrange & Act
        var user1 = UserFaker.Generate();
        var user2 = UserFaker.Generate();

        // Assert
        Assert.NotEqual(user1.Id, user2.Id);
        Assert.NotEqual(user1.Email, user2.Email);
        Assert.NotEqual(user1.Username, user2.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var faker = new Bogus.Faker();

        // Act
        var act = () => new User(invalidName!, faker.Internet.Email(), "john_doe", faker.Internet.Password());

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Name", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    public void Constructor_ShouldThrow_WhenEmailIsInvalid(string? invalidEmail)
    {
        // Arrange
        var faker = new Bogus.Faker();

        // Act
        var act = () => new User(faker.Name.FullName(), invalidEmail!, "john_doe", faker.Internet.Password());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]                    // too short
    [InlineData("A_BC")]                 // uppercase
    [InlineData("john-doe")]             // hyphen
    [InlineData("john doe")]             // space
    [InlineData("john@doe")]             // special char
    public void Constructor_ShouldThrow_WhenUsernameIsInvalid(string? invalidUsername)
    {
        // Arrange
        var faker = new Bogus.Faker();

        // Act
        var act = () => new User(faker.Name.FullName(), faker.Internet.Email(), invalidUsername!, faker.Internet.Password());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenPasswordHashIsInvalid(string? invalidPassword)
    {
        // Arrange
        var faker = new Bogus.Faker();

        // Act
        var act = () => new User(faker.Name.FullName(), faker.Internet.Email(), "john_doe", invalidPassword!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void SetUsername_ShouldUpdate_WhenValid()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetUsername("new_username123");

        // Assert
        Assert.Equal("new_username123", user.Username);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalseAndDeletedAt()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.IsActive);
        Assert.NotNull(user.DeletedAt);
        Assert.True(user.DeletedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void SetLastLogin_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetLastLogin();

        // Assert
        Assert.NotNull(user.LastLoginAt);
        Assert.True(user.LastLoginAt <= DateTime.UtcNow);
    }

    [Fact]
    public void SetPasswordResetToken_ShouldStoreTokenWithExpiry()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetPasswordResetToken("reset-token-123");

        // Assert
        Assert.Equal("reset-token-123", user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetExpiry);
        Assert.True(user.PasswordResetExpiry > DateTime.UtcNow);
        Assert.True(user.IsPasswordResetTokenValid());
    }

    [Fact]
    public void ClearPasswordResetToken_ShouldRemoveTokenAndExpiry()
    {
        // Arrange
        var user = UserFaker.Generate();
        user.SetPasswordResetToken("reset-token-123");

        // Act
        user.ClearPasswordResetToken();

        // Assert
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetExpiry);
    }

    [Fact]
    public void SetBirthDate_ShouldThrow_WhenInFuture()
    {
        // Arrange
        var user = UserFaker.Generate();
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Act
        var act = () => user.SetBirthDate(future);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Birth date", exception.Message);
    }

    [Fact]
    public void SetBirthDate_ShouldSet_WhenValid()
    {
        // Arrange
        var user = UserFaker.Generate();
        var birthDate = new DateOnly(1990, 5, 15);

        // Act
        user.SetBirthDate(birthDate);

        // Assert
        Assert.Equal(birthDate, user.BirthDate);
    }

    [Fact]
    public void SetBirthDate_ShouldClear_WhenNull()
    {
        // Arrange
        var user = UserFaker.Generate();
        user.SetBirthDate(new DateOnly(1990, 5, 15));

        // Act
        user.SetBirthDate(null);

        // Assert
        Assert.Null(user.BirthDate);
    }

    [Fact]
    public void SetLocation_ShouldTrimValues()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetLocation("  São Paulo  ", "  SP  ");

        // Assert
        Assert.Equal("São Paulo", user.City);
        Assert.Equal("SP", user.State);
    }

    [Fact]
    public void SetLocation_ShouldSetNull_WhenWhitespace()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetLocation("   ", "");

        // Assert
        Assert.Null(user.City);
        Assert.Null(user.State);
    }

    [Fact]
    public void SetRelationshipStatus_ShouldSetValue()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetRelationshipStatus(RelationshipStatus.Married);

        // Assert
        Assert.Equal(RelationshipStatus.Married, user.RelationshipStatus);
    }

    [Fact]
    public void SetRelationshipStatus_ShouldClear_WhenNull()
    {
        // Arrange
        var user = UserFaker.Generate();
        user.SetRelationshipStatus(RelationshipStatus.Single);

        // Act
        user.SetRelationshipStatus(null);

        // Assert
        Assert.Null(user.RelationshipStatus);
    }

    [Fact]
    public void SetInterests_ShouldTrimAndDeduplicate()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetInterests(
            new[] { " Rock ", "rock", " Pop " },
            new[] { " Action ", "" },
            new[] { "  ", "Fiction" },
            new[] { " Reading ", "reading" });

        // Assert
        Assert.Equal(new[] { "Rock", "Pop" }, user.MusicInterests);
        Assert.Equal(new[] { "Action" }, user.MovieInterests);
        Assert.Equal(new[] { "Fiction" }, user.BookInterests);
        Assert.Equal(new[] { "Reading" }, user.Hobbies);
    }

    [Fact]
    public void SetInterests_ShouldSetNull_WhenEmptyOrWhitespace()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        user.SetInterests(
            new[] { "  " },
            Array.Empty<string>(),
            null,
            new[] { "", "  " });

        // Assert
        Assert.Null(user.MusicInterests);
        Assert.Null(user.MovieInterests);
        Assert.Null(user.BookInterests);
        Assert.Null(user.Hobbies);
    }
}
