using App.Application.DTOs;
using App.Application.Exceptions;
using App.Application.Interfaces;
using App.Application.Services;
using App.Application.Test.Fakers;
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
        Assert.Equal(request.Email, result.Email);

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
    public async Task CreateAsync_ShouldThrowValidationException_WhenPasswordIsInvalid(string? invalidPassword)
    {
        // Arrange
        var request = new CreateUserRequest("John Doe", "john@example.com", invalidPassword!);

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
    public async Task UpdateAsync_ShouldThrowValidationException_WhenPasswordIsInvalid(string? invalidPassword)
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
}
