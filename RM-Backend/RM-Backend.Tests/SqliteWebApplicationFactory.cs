using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RequestsManagement.Data;

namespace RM_Backend.Tests
{
    /// <summary>
    /// Runs the API against a real relational provider (SQLite, temp file per factory).
    /// Unlike InMemory, the UPDATE is real SQL with "WHERE Id = @id AND Version = @original",
    /// so the EF/DB-level concurrency token is enforced by the database itself.
    /// A file (not in-memory) DB is used so parallel requests each get their own connection.
    /// </summary>
    public class SqliteWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"rm-tests-{Guid.NewGuid():N}.db");

        public string ConnectionString => $"Data Source={_dbPath}";

        /// <summary>Lets a test run code right before the next SaveChanges (simulated race).</summary>
        public RaceSimulationInterceptor RaceSimulator { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove the SQL Server registration from Program.cs (options + configuration callbacks)
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                services.AddDbContext<AppDbContext>(options => options
                    .UseSqlite(ConnectionString)
                    .AddInterceptors(RaceSimulator));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

            return host;
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            SqliteConnection.ClearAllPools();
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Temp file - not worth failing a test over
            }

            GC.SuppressFinalize(this);
        }
    }
}
