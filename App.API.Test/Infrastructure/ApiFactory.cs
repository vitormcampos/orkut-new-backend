using App.Application.Interfaces;
using App.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace App.API.Test.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"api-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", "test-secret-key-with-at-least-32-characters!!");
        builder.UseSetting("Jwt:Issuer", "OrkutNew.Tests");
        builder.UseSetting("Jwt:Audience", "OrkutNew.Tests");
        builder.UseSetting("Database:UseInMemory", "true");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddScoped<DbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());

            services.RemoveAll<IStorageService>();
            services.AddScoped<IStorageService, TestStorageService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        });
    }
}

public sealed class TestStorageService : IStorageService
{
    public Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://storage.test/{fileName}");
    }
}
