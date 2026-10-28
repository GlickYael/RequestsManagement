using RequestsManagement.Data;
using RequestsManagement.Enums;
using RequestsManagement.Models;

namespace RequestsManagement.Services
{
    public class SeedService : ISeedService
    {
        private readonly AppDbContext _context;

        public SeedService(AppDbContext context)
        {
            _context = context;
        }

        public void SeedData(bool forceRecreate)
        {
            if (forceRecreate && _context.Requests.Any())
            {
                _context.Requests.RemoveRange(_context.Requests);
                _context.SaveChanges();
            }

            var requestsList = new List<RequestEntity>();

            for (int i = 1; i <= 100000; i++)
            {
                requestsList.Add(new RequestEntity
                {
                    Title = $"Request Title {i}",
                    OrganizationName = $"Org {(i % 500) + 1}",
                    Status = (RequestStatus)(i % 4),
                    Priority = (RequestPriority)(i % 3),
                    AssignedTo = $"Developer {(i % 10) + 1}",
                    CreatedAt = DateTime.UtcNow.AddDays(-i % 100)
                });

                if (requestsList.Count == 5000)
                {
                    _context.Requests.AddRange(requestsList);
                    _context.SaveChanges();
                    requestsList.Clear();
                }
            }
        }
    }
}