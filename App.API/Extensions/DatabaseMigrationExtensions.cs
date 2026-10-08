using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.API.Extensions;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
