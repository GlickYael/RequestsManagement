using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.DTOs;
using RequestsManagement.Models;

namespace RequestsManagement.Services
{
    public class RequestStatusAuditService : IRequestStatusAuditService
    {
        private readonly AppDbContext _context;

        public RequestStatusAuditService(AppDbContext context)
        {
            _context = context;
        }

        public Task AddAuditAsync(int requestId, string previousStatus, string newStatus, CancellationToken cancellationToken)
        {
            var auditEntry = new RequestStatusAuditEntity
            {
                RequestId = requestId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "CurrentAdmin"
            };

            _context.RequestStatusAudits.Add(auditEntry);
            // SaveChanges is deliberately not called here, so this is persisted in the same
            // transaction as the request update
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<RequestStatusAuditDto>> GetAuditByRequestIdAsync(int requestId, CancellationToken cancellationToken)
        {
            return await _context.RequestStatusAudits
                .Where(a => a.RequestId == requestId)
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new RequestStatusAuditDto
                {
                    Id = a.Id,
                    RequestId = a.RequestId,
                    PreviousStatus = a.PreviousStatus,
                    NewStatus = a.NewStatus,
                    ChangedAt = a.ChangedAt,
                    ChangedBy = a.ChangedBy
                })
                .ToListAsync(cancellationToken);
        }
    }
}
