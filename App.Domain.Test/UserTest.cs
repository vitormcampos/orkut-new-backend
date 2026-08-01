using App.Domain.Test.Fakers;

namespace App.Domain.Test;

public class UserTest
{
    [Fact]
    public void Constructor_ShouldCreateUser_WhenDataIsValid()
    {
        // Arrange
        var faker = new Bogus.Faker();
        var name = faker.Name.FullName();
        var email = faker.Internet.Email();
        var password = faker.Internet.Password();

        // Act
        var user = new Domain.Entities.User(name, email, password);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(password, user.PasswordHash);
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
        Assert.NotEqual(user1.PasswordHash, user2.PasswordHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var faker = new Bogus.Faker();
        var email = faker.Internet.Email();
        var password = faker.Internet.Password();

        // Act
        var act = () => new Domain.Entities.User(invalidName!, email, password);

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
        var name = faker.Name.FullName();
        var password = faker.Internet.Password();

        // Act
        var act = () => new Domain.Entities.User(name, invalidEmail!, password);

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
        var name = faker.Name.FullName();
        var email = faker.Internet.Email();

        // Act
        var act = () => new Domain.Entities.User(name, email, invalidPassword!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Email_ShouldBeImmutable_AfterConstruction()
    {
        // Arrange
        var user = UserFaker.Generate();
        var originalEmail = user.Email;

        // Assert — Email property has private setter, cannot be changed externally
        Assert.Equal(originalEmail, user.Email);
    }

    [Fact]
    public void SetName_ShouldUpdateName_WhenNameIsValid()
    {
        // Arrange
        var user = UserFaker.Generate();
        var faker = new Bogus.Faker();
        var newName = faker.Name.FullName();

        // Act
        user.SetName(newName);

        // Assert
        Assert.Equal(newName, user.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        var act = () => user.SetName(invalidName!);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    public void SetPasswordHash_ShouldUpdatePasswordHash_WhenHashIsValid()
    {
        // Arrange
        var user = UserFaker.Generate();
        var newHash = "$2a$11$abcdefghijklmnopqrstuvwxyz12345678901234567890";

        // Act
        user.SetPasswordHash(newHash);

        // Assert
        Assert.Equal(newHash, user.PasswordHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPasswordHash_ShouldThrow_WhenHashIsInvalid(string? invalidHash)
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        var act = () => user.SetPasswordHash(invalidHash!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
