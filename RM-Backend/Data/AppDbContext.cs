using Microsoft.EntityFrameworkCore;
using RequestsManagement.Enums;
using RequestsManagement.Models;

namespace RequestsManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<RequestEntity> Requests { get; set; }

        public DbSet<RequestStatusAuditEntity> RequestStatusAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // OrganizationName/AssignedTo are filtered only via Contains() (leading-wildcard
            // substring search), which can't use a B-tree index seek either way - and
            // OrganizationName is no longer a sortable field, so neither earns its write cost.
            // No index is defined for them.

            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => r.Priority);

            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => r.CreatedAt);

            // Composite index for the most common query pattern: filter by Status + sort by
            // CreatedAt (the dashboard's default). Without it, SQL Server has to scan via
            // IX_Requests_CreatedAt and do a Key Lookup on every row to check Status -
            // see the performance measurement in README.
            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => new { r.Status, r.CreatedAt });

            // Same pattern for the Priority filter: filter by Priority + default sort by CreatedAt
            // becomes an index seek instead of a scan + Key Lookup.
            modelBuilder.Entity<RequestEntity>()
                .HasIndex(r => new { r.Priority, r.CreatedAt });

            // Version as an EF/DB-level concurrency token: the generated UPDATE includes the
            // original version in its WHERE clause, so if a row was already changed between
            // load and save, 0 rows are affected and EF throws DbUpdateConcurrencyException -
            // closing the race window between the manual check and the save.
            modelBuilder.Entity<RequestEntity>()
                .Property(r => r.Version)
                .IsConcurrencyToken();

            // CHECK constraints so the DB itself rejects a Status/Priority value outside the
            // enum's range, independent of the application layer. Derived from the enums
            // (not hardcoded) so they stay correct if a member is ever added - this assumes
            // the enums stay contiguous from 0, which they are today.
            var statusValues = Enum.GetValues<RequestStatus>().Cast<int>();
            modelBuilder.Entity<RequestEntity>().ToTable(t => t.HasCheckConstraint(
                "CK_Requests_Status_ValidEnum",
                $"[Status] BETWEEN {statusValues.Min()} AND {statusValues.Max()}"));

            var priorityValues = Enum.GetValues<RequestPriority>().Cast<int>();
            modelBuilder.Entity<RequestEntity>().ToTable(t => t.HasCheckConstraint(
                "CK_Requests_Priority_ValidEnum",
                $"[Priority] BETWEEN {priorityValues.Min()} AND {priorityValues.Max()}"));
        }
    }
}