using App.Application.DTOs;
using App.Application.Exceptions;
using App.Application.Interfaces;
using App.Application.Services;
using App.Application.Test.Fakers;
using App.Domain.Entities;
using App.Domain.Exceptions;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace App.Application.Test;

public class UserServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserService _userService;

    public UserServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _userService = new UserService(_context, _passwordHasher);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserDto_WhenUserExists()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
    {
        // Arrange
        var user = new User("Email User", "email@test.com", "email_user", "hash");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByEmailAsync(user.Email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnNull_WhenUsernameDoesNotExist()
    {
        // Arrange

        // Act
        var result = await _userService.GetByUsernameAsync("missing_user");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenUsernameExists()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByUsernameAsync(user.Username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var users = UserFaker.GenerateUsers(3);
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoUsersExist()
    {
        // Arrange — empty context

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnActiveUsersByNameOrUsername_CaseInsensitively()
    {
        // Arrange
        var ana = new User("Ana Silva", "ana@example.com", "ana_silva", "Test@123");
        var inactive = new User("Ana Inactive", "inactive@example.com", "ana_inactive", "Test@123");
        inactive.Deactivate();
        var unrelated = new User("Bruno", "bruno@example.com", "bruno", "Test@123");
        _context.Users.AddRange(ana, inactive, unrelated);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.SearchAsync("  ANA  ");

        // Assert
        var match = Assert.Single(result);
        Assert.Equal(ana.Id, match.Id);
        Assert.Equal("ana_silva", match.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    public async Task SearchAsync_ShouldThrowValidationException_WhenTermIsInvalid(string? term)
    {
        // Arrange

        // Act
        var act = () => _userService.SearchAsync(term!, 10);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(act);
    }

    [Fact]
    public async Task SearchAsync_ShouldClampLimitToTwenty()
    {
        // Arrange
        var users = Enumerable
            .Range(1, 21)
            .Select(i => new User(
                $"Person {i:00}",
                $"person{i}@example.com",
                $"person_{i}",
                "Test@123"
            ));
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.SearchAsync("person", 25);

        // Assert
        Assert.Equal(20, result.Count);
    }

    [Fact]
    public async Task CreateAsync_ShouldHashPasswordAndReturnCreatedUserDto()
    {
        // Arrange
        var request = UserFaker.GenerateCreateRequest();
        var fakeHash = "$2a$11$faketesthash1234567890123456789012345678901";

        _passwordHasher.Hash(request.Password).Returns(fakeHash);

        // Act
        var result = await _userService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Email.ToLowerInvariant(), result.Email);

        _passwordHasher.Received(1).Hash(request.Password);

        var savedUser = await _context.Users.FindAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(fakeHash, savedUser!.PasswordHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("abcde")]
    public async Task CreateAsync_ShouldThrowValidationException_WhenPasswordIsInvalid(
        string? invalidPassword
    )
    {
        // Arrange
        var request = new CreateUserRequest(
            "John Doe",
            "john@example.com",
            "john_doe",
            invalidPassword!
        );

        // Act
        var act = () => _userService.CreateAsync(request);

        // Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(act);
        Assert.Contains("6 characters", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNameAndPasswordHash_WhenUserExists()
    {
        // Arrange
        var existingUser = UserFaker.GenerateUser();
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var updateRequest = UserFaker.GenerateUpdateRequest();
        var newFakeHash = "$2a$11$updatedhash123456789012345678901234567890";

        _passwordHasher.Hash(updateRequest.Password).Returns(newFakeHash);

        // Act
        var result = await _userService.UpdateAsync(existingUser.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Equal(existingUser.Email, result.Email); // email unchanged

        _passwordHasher.Received(1).Hash(updateRequest.Password);

        var updatedUser = await _context.Users.FindAsync(existingUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(newFakeHash, updatedUser!.PasswordHash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("abcde")]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenPasswordIsInvalid(
        string? invalidPassword
    )
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateUserRequest("New Name", invalidPassword!);

        // Act
        var act = () => _userService.UpdateAsync(user.Id, updateRequest);

        // Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(act);
        Assert.Contains("6 characters", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = UserFaker.GenerateUpdateRequest();

        // Act
        var act = () => _userService.UpdateAsync(userId, updateRequest);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldPersistNewProfileFields()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = UserFaker.GenerateUpdateProfileRequest();

        // Act
        var result = await _userService.UpdateProfileAsync(user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.BirthDate, result.BirthDate);
        Assert.Equal(request.RelationshipStatus, result.RelationshipStatus);
        Assert.NotNull(result.MusicInterests);
        Assert.NotNull(result.MovieInterests);
        Assert.NotNull(result.BookInterests);
        Assert.NotNull(result.Hobbies);

        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(request.BirthDate, updatedUser!.BirthDate);
        Assert.Equal(request.City, updatedUser.City);
        Assert.Equal(request.State, updatedUser.State);
        Assert.Equal(request.RelationshipStatus, updatedUser.RelationshipStatus?.ToString());
        Assert.NotNull(updatedUser.MusicInterests);
        Assert.NotNull(updatedUser.MovieInterests);
        Assert.NotNull(updatedUser.BookInterests);
        Assert.NotNull(updatedUser.Hobbies);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("999")]
    public async Task UpdateProfileAsync_ShouldThrowValidationException_WhenRelationshipStatusIsInvalid(
        string invalidStatus
    )
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateProfileRequest(
            "Name",
            null,
            null,
            null,
            null,
            null,
            invalidStatus,
            null,
            null,
            null,
            null
        );

        // Act
        var act = () => _userService.UpdateProfileAsync(user.Id, request);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(act);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldClearFields_WhenNullProvided()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.SetBirthDate(new DateOnly(1990, 1, 1));
        user.SetLocation("São Paulo", "SP");
        user.SetRelationshipStatus(RelationshipStatus.Married);
        user.SetInterests(
            new[] { "Rock" },
            new[] { "Action" },
            new[] { "Fiction" },
            new[] { "Reading" }
        );
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateProfileRequest(
            "Name",
            user.Username,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        // Act
        await _userService.UpdateProfileAsync(user.Id, request);

        // Assert
        var updated = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Null(updated!.BirthDate);
        Assert.Null(updated.City);
        Assert.Null(updated.State);
        Assert.Null(updated.RelationshipStatus);
        Assert.Null(updated.MusicInterests);
        Assert.Null(updated.MovieInterests);
        Assert.Null(updated.BookInterests);
        Assert.Null(updated.Hobbies);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser_WhenUserExists()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.DeleteAsync(user.Id);

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = () => _userService.DeleteAsync(userId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateProfileFields()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var request = new UpdateProfileRequest(
            "Updated",
            "updated_user",
            "New bio",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        // Act
        var result = await _userService.UpdateProfileAsync(user.Id, request);

        // Assert
        Assert.Equal("Updated", result.Name);
        Assert.Equal("updated_user", result.Username);
        Assert.Equal("New bio", result.Bio);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrow_WhenUsernameAlreadyExists()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        var other = UserFaker.GenerateUser();
        _context.Users.AddRange(user, other);
        await _context.SaveChangesAsync();
        var request = new UpdateProfileRequest(
            user.Name,
            other.Username,
            user.Bio,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        // Act
        var act = () => _userService.UpdateProfileAsync(user.Id, request);

        // Assert
        await Assert.ThrowsAsync<UsernameAlreadyTakenException>(act);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new UpdateProfileRequest(
            "Name",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        // Act
        var act = () => _userService.UpdateProfileAsync(Guid.NewGuid(), request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldDeactivateUser()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.DeactivateAsync(user.Id);

        // Assert
        var stored = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
        Assert.NotNull(stored.DeletedAt);
    }

    [Fact]
    public async Task SetProfilePictureAsync_ShouldUpdatePicture()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.SetProfilePictureAsync(
            user.Id,
            "https://img.test/avatar.png"
        );

        // Assert
        Assert.Equal("https://img.test/avatar.png", result.ProfilePicture);
    }

    [Fact]
    public async Task SetPasswordResetTokenAsync_ShouldPersistToken()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userService.SetPasswordResetTokenAsync(user, "reset-token");

        // Assert
        var stored = await _context.Users.FindAsync(user.Id);
        Assert.Equal("reset-token", stored!.PasswordResetToken);
    }

    [Fact]
    public async Task GetByPasswordResetTokenAsync_ShouldReturnUser()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.SetPasswordResetToken("reset-token");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetByPasswordResetTokenAsync("reset-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldHashPasswordAndClearToken()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.SetPasswordResetToken("reset-token");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _passwordHasher.Hash("NewPassword123!").Returns("new-hash");

        // Act
        await _userService.ResetPasswordAsync(user, "NewPassword123!");

        // Assert
        var stored = await _context.Users.FindAsync(user.Id);
        Assert.Equal("new-hash", stored!.PasswordHash);
        Assert.Null(stored.PasswordResetToken);
    }
}
