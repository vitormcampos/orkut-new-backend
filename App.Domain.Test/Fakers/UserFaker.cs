using App.Domain.Entities;
using Bogus;

namespace App.Domain.Test.Fakers;

public static class UserFaker
{
    private static readonly Faker<User> Faker = new Faker<User>()
        .CustomInstantiator(f => new User(
            f.Name.FullName(),
            f.Internet.Email(),
            f.Internet.UserName().ToLower().Replace(".", "_"),
            f.Internet.Password()
        ));

    public static User Generate() => Faker.Generate();

    public static List<User> Generate(int count) => Faker.Generate(count);
}
