using App.Application.Interfaces;
using App.Application.Services;
using App.Infrastructure.Data;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.IOC;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IStorageService, CloudflareR2StorageService>();

        return services;
    }

    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.AddScoped<IFriendshipService, FriendshipService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICommunityService, CommunityService>();

        return services;
    }
}
