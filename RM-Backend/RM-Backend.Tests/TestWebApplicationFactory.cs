using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Data;

namespace RM_Backend.Tests
{
    /// <summary>
    /// Swaps the DbContext registration from SQL Server to an InMemory database unique
    /// to each instance, so every test gets its own clean, isolated database without
    /// touching the real LocalDB.
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // The AddDbContext(SqlServer) call that already ran in Program.cs leaves
                // provider-level SqlServer registrations in the IServiceCollection even after
                // removing the DbContextOptions itself. To keep EF from seeing two providers
                // registered at once, InMemory gets its own isolated service provider instead
                // of letting EF scan the shared IServiceCollection.
                var inMemoryServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.UseInternalServiceProvider(inMemoryServiceProvider);
                });
            });
        }
    }
}
