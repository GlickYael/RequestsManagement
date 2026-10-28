using RequestsManagement.DTOs;

namespace RequestsManagement.Services
{
    public interface IRequestStatusAuditService
    {
        Task AddAuditAsync(int requestId, string previousStatus, string newStatus, CancellationToken cancellationToken);
        Task<IEnumerable<RequestStatusAuditDto>> GetAuditByRequestIdAsync(int requestId, CancellationToken cancellationToken);
    }
}
