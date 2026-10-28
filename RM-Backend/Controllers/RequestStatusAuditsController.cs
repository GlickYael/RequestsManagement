using Microsoft.AspNetCore.Mvc;
using RequestsManagement.DTOs;
using RequestsManagement.Services;

namespace RequestsManagement.Controllers
{
    [Route("api/requests/{id}/status-audit")]
    [ApiController]
    public class RequestStatusAuditsController : ControllerBase
    {
        private readonly IRequestStatusAuditService _auditService;

        public RequestStatusAuditsController(IRequestStatusAuditService auditService)
        {
            _auditService = auditService;
        }

        // GET: api/requests/{id}/status-audit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RequestStatusAuditDto>>> GetStatusAudit(
            int id,
            CancellationToken cancellationToken)
        {
            var auditTrail = await _auditService.GetAuditByRequestIdAsync(id, cancellationToken);

            return Ok(auditTrail);
        }
    }
}