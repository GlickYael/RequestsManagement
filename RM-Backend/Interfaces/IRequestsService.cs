using RequestsManagement.DTOs;
using RequestsManagement.Enums;

namespace RequestsManagement.Services
{
    public interface IRequestsService
    {
        Task<RequestsPageResultDto> GetPageRequestsAsync(RequestsQueryParameters parameters, CancellationToken cancellationToken);

        Task<UpdateStatusResult> UpdateStatusAsync(int id, UpdateStatusDto dto, CancellationToken cancellationToken);

        Task<List<BulkUpdateFailureDto>> BulkUpdateStatusAsync(BulkUpdateStatusDto dto, CancellationToken cancellationToken);
    }
}