using App.Application.DTOs;
using App.Domain.Entities;
using Bogus;

namespace App.Application.Test.Fakers;

public static class UserFaker
{
    private const string DefaultPassword = "Test@123";

    private static readonly Faker<User> UserEntityFaker = new Faker<User>()
        .CustomInstantiator(f => new User(
            f.Name.FullName(),
            f.Internet.Email(),
            f.Internet.Password()
        ));

    private static readonly Faker<CreateUserRequest> CreateRequestFaker = new Faker<CreateUserRequest>()
        .CustomInstantiator(f => new CreateUserRequest(
            f.Name.FullName(),
            f.Internet.Email(),
            DefaultPassword
        ));

    private static readonly Faker<UpdateUserRequest> UpdateRequestFaker = new Faker<UpdateUserRequest>()
        .CustomInstantiator(f => new UpdateUserRequest(
            f.Name.FullName(),
            DefaultPassword
        ));

    public static User GenerateUser() => UserEntityFaker.Generate();
    public static List<User> GenerateUsers(int count) => UserEntityFaker.Generate(count);
    public static CreateUserRequest GenerateCreateRequest() => CreateRequestFaker.Generate();
    public static UpdateUserRequest GenerateUpdateRequest() => UpdateRequestFaker.Generate();
}
