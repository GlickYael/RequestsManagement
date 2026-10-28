using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Data;
using RequestsManagement.DTOs;
using RequestsManagement.Enums;
using RequestsManagement.Models;

namespace RM_Backend.Tests
{
    /// <summary>
    /// Concurrency tests against a real relational DB (SQLite). These cover the EF/DB-level
    /// concurrency token (Version in the UPDATE's WHERE clause) - the layer that closes the
    /// race window the manual Version check cannot. Removing .IsConcurrencyToken() from
    /// AppDbContext makes the race tests below fail.
    /// </summary>
    public class ConcurrencyIntegrationTests
    {
        private static async Task<int> SeedAsync(SqliteWebApplicationFactory factory, RequestStatus status, int version = 1)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = new RequestEntity
            {
                Title = "Race",
                OrganizationName = "Org",
                Status = status,
                Priority = RequestPriority.Medium,
                CreatedAt = DateTime.UtcNow,
                Version = version
            };
            context.Requests.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private static async Task<(RequestEntity Request, List<RequestStatusAuditEntity> Audits)> LoadAsync(
            SqliteWebApplicationFactory factory, int id)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var request = await context.Requests.AsNoTracking().SingleAsync(r => r.Id == id);
            var audits = await context.RequestStatusAudits.AsNoTracking().Where(a => a.RequestId == id).ToListAsync();
            return (request, audits);
        }

        /// <summary>
        /// Another user's write, sent on its own connection (bypassing the service and EF).
        /// </summary>
        private static async Task CompetingUpdateAsync(SqliteWebApplicationFactory factory, int id, RequestStatus newStatus)
        {
            await using var connection = new SqliteConnection(factory.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Requests SET Status = $status, Version = Version + 1 WHERE Id = $id";
            command.Parameters.AddWithValue("$status", (int)newStatus);
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task UpdateStatus_RowChangedAfterManualCheck_DbConcurrencyTokenReturnsConflict()
        {
            await using var factory = new SqliteWebApplicationFactory();
            var id = await SeedAsync(factory, RequestStatus.InProgress, version: 1);

            // The service loads version 1 and passes the manual check (client also sent 1).
            // Right before its SaveChanges, another user moves the row to Waiting / version 2.
            factory.RaceSimulator.RunOnceBeforeNextSave(() => CompetingUpdateAsync(factory, id, RequestStatus.Waiting));

            var client = factory.CreateClient();
            var response = await client.PatchAsync(
                $"/api/requests/{id}/status",
                JsonContent.Create(new { status = "Completed", version = 1 }));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            // The competing write survived - it was not silently overwritten (no lost update)
            var (request, audits) = await LoadAsync(factory, id);
            Assert.Equal(RequestStatus.Waiting, request.Status);
            Assert.Equal(2, request.Version);
            Assert.Empty(audits);
        }

        [Fact]
        public async Task UpdateStatus_ParallelRequestsWithSameVersion_ExactlyOneSucceeds()
        {
            await using var factory = new SqliteWebApplicationFactory();
            var id = await SeedAsync(factory, RequestStatus.New, version: 1);
            var client = factory.CreateClient();

            const int parallelRequests = 10;
            var responses = await Task.WhenAll(Enumerable.Range(0, parallelRequests).Select(_ =>
                client.PatchAsync(
                    $"/api/requests/{id}/status",
                    JsonContent.Create(new { status = "InProgress", version = 1 }))));

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.NoContent));
            Assert.Equal(parallelRequests - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

            var (request, audits) = await LoadAsync(factory, id);
            Assert.Equal(2, request.Version);
            Assert.Single(audits);
        }

        [Fact]
        public async Task BulkUpdate_DbConflictOnOneItem_OtherItemsStillSucceed()
        {
            await using var factory = new SqliteWebApplicationFactory();
            var firstId = await SeedAsync(factory, RequestStatus.InProgress, version: 1);
            var secondId = await SeedAsync(factory, RequestStatus.InProgress, version: 1);

            // Race on the first item's save only
            factory.RaceSimulator.RunOnceBeforeNextSave(() => CompetingUpdateAsync(factory, firstId, RequestStatus.Waiting));

            var client = factory.CreateClient();
            var response = await client.PatchAsync(
                "/api/requests/batch-status",
                JsonContent.Create(new
                {
                    status = "Completed",
                    items = new[]
                    {
                        new { id = firstId, version = 1, oldStatus = "InProgress" },
                        new { id = secondId, version = 1, oldStatus = "InProgress" }
                    }
                }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var failures = await response.Content.ReadFromJsonAsync<List<BulkUpdateFailureDto>>();
            var failure = Assert.Single(failures!);
            Assert.Equal(firstId, failure.Id);
            Assert.Equal(UpdateStatusResult.Conflict, failure.ErrorReason);

            // The failed item's pending changes must not leak into the next item's save
            var (second, secondAudits) = await LoadAsync(factory, secondId);
            Assert.Equal(RequestStatus.Completed, second.Status);
            Assert.Equal(2, second.Version);
            Assert.Single(secondAudits);
        }

        [Fact]
        public async Task UpdateStatus_SameStatus_ReturnsNoContentWithoutWriting()
        {
            await using var factory = new SqliteWebApplicationFactory();
            var id = await SeedAsync(factory, RequestStatus.InProgress, version: 3);
            var client = factory.CreateClient();

            var response = await client.PatchAsync(
                $"/api/requests/{id}/status",
                JsonContent.Create(new { status = "InProgress", version = 3 }));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var (request, audits) = await LoadAsync(factory, id);
            Assert.Equal(3, request.Version);
            Assert.Null(request.UpdatedAt);
            Assert.Empty(audits);
        }
    }
}
