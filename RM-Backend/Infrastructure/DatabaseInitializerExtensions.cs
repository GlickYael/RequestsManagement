using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.Services;

namespace RequestsManagement.Infrastructure
{
    public static class DatabaseInitializerExtensions
    {
        public static void InitializeDatabase(this WebApplication app, string[] args)
        {
            // Integration tests create their own schema (InMemory / SQLite) - the migrations
            // are SQL Server specific, so they are not applied there.
            if (app.Environment.IsEnvironment("Testing"))
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                context.Database.Migrate();

                if (args.Contains("seedTestData"))
                {
                    // seedTestData alone: appends another 100,000 deterministic records (on top of any existing data).
                    // seedTestData forceReseed: wipes everything first, then inserts 100,000 deterministic records from scratch.
                    var seedService = services.GetRequiredService<ISeedService>();
                    seedService.SeedData(forceRecreate: args.Contains("forceReseed"));
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(DatabaseInitializerExtensions));
                logger.LogError(ex, "An error occurred while creating/migrating the database or seeding data.");
            }
        }
    }
}
