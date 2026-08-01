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
}
