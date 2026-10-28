using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Data;
using RequestsManagement.DTOs;
using RequestsManagement.Enums;
using RequestsManagement.Models;

namespace RM_Backend.Tests
{
    /// <summary>
    /// End-to-end integration tests: real HTTP via WebApplicationFactory, exercising
    /// Controller->Service->DbContext (InMemory)->Serialization - not isolated unit tests.
    /// </summary>
    public class RequestsApiIntegrationTests
    {
        private static RequestEntity NewRequest(
            string title,
            string org,
            RequestStatus status,
            RequestPriority priority = RequestPriority.Medium,
            int version = 1) => new()
        {
            Title = title,
            OrganizationName = org,
            Status = status,
            Priority = priority,
            AssignedTo = "Tester",
            CreatedAt = DateTime.UtcNow,
            Version = version
        };

        [Fact]
        public async Task GetRequests_FiltersByStatus_ReturnsOnlyMatchingItems()
        {
            await using var factory = new TestWebApplicationFactory();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Requests.AddRange(
                    NewRequest("Alpha", "Org A", RequestStatus.New),
                    NewRequest("Beta", "Org B", RequestStatus.Completed)
                );
                await context.SaveChangesAsync();
            }

            var client = factory.CreateClient();
            var response = await client.GetAsync("/api/requests?PageNumber=1&PageSize=10&SortBy=CreatedAt&Status=New");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<RequestsPageResultDto>();
            Assert.NotNull(body);
            Assert.Equal(1, body!.TotalCount);
            Assert.Single(body.Items);
            Assert.Equal("Alpha", body.Items.First().Title);
        }

        [Fact]
        public async Task GetRequests_InvalidSortByValue_ReturnsBadRequest()
        {
            await using var factory = new TestWebApplicationFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/requests?PageNumber=1&PageSize=10&SortBy=NotARealField");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_ValidTransitionAndVersion_ReturnsNoContent()
        {
            await using var factory = new TestWebApplicationFactory();
            int id;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var entity = NewRequest("Gamma", "Org C", RequestStatus.New, version: 1);
                context.Requests.Add(entity);
                await context.SaveChangesAsync();
                id = entity.Id;
            }

            var client = factory.CreateClient();
            var response = await client.PatchAsync(
                $"/api/requests/{id}/status",
                JsonContent.Create(new { status = "InProgress", version = 1 }));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        // Covers only the manual Version check (stale client data). The DB-level concurrency
        // token (the real race window) is covered in ConcurrencyIntegrationTests against SQLite.
        public async Task UpdateStatus_StaleClientVersion_ManualCheckReturnsConflict()
        {
            await using var factory = new TestWebApplicationFactory();
            int id;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var entity = NewRequest("Delta", "Org D", RequestStatus.New, version: 5);
                context.Requests.Add(entity);
                await context.SaveChangesAsync();
                id = entity.Id;
            }

            var client = factory.CreateClient();
            // Version 1 is sent while the actual version is 5 - simulates a competing edit (lost-update scenario)
            var response = await client.PatchAsync(
                $"/api/requests/{id}/status",
                JsonContent.Create(new { status = "InProgress", version = 1 }));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_DisallowedTransition_ReturnsBadRequest()
        {
            await using var factory = new TestWebApplicationFactory();
            int id;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Completed is a terminal status - no transition out of it is allowed (see AllowedTransitions in RequestsService)
                var entity = NewRequest("Epsilon", "Org E", RequestStatus.Completed, version: 1);
                context.Requests.Add(entity);
                await context.SaveChangesAsync();
                id = entity.Id;
            }

            var client = factory.CreateClient();
            var response = await client.PatchAsync(
                $"/api/requests/{id}/status",
                JsonContent.Create(new { status = "New", version = 1 }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task BulkUpdateStatus_DisallowedTransitionForOneItem_ReturnsItInFailuresWithInvalidStatus()
        {
            await using var factory = new TestWebApplicationFactory();
            int completedId, newId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Completed is terminal - a request to move it to Waiting should fail and land in the failures list,
                // without blocking the other (valid) item in the same batch - this is what Partial Success means.
                // (Only InProgress -> Waiting is allowed per AllowedTransitions, so the second item starts InProgress.)
                var completed = NewRequest("Zeta", "Org Z", RequestStatus.Completed, version: 1);
                var inProgress = NewRequest("Eta", "Org H", RequestStatus.InProgress, version: 1);
                context.Requests.AddRange(completed, inProgress);
                await context.SaveChangesAsync();
                completedId = completed.Id;
                newId = inProgress.Id;
            }

            var client = factory.CreateClient();
            var response = await client.PatchAsync(
                "/api/requests/batch-status",
                JsonContent.Create(new
                {
                    status = "Waiting",
                    items = new[]
                    {
                        new { id = completedId, version = 1, oldStatus = "Completed" },
                        new { id = newId, version = 1, oldStatus = "InProgress" }
                    }
                }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var failures = await response.Content.ReadFromJsonAsync<List<BulkUpdateFailureDto>>();
            Assert.NotNull(failures);
            var failure = Assert.Single(failures!);
            Assert.Equal(completedId, failure.Id);
            Assert.Equal(UpdateStatusResult.InvalidStatus, failure.ErrorReason);
        }
    }
}
