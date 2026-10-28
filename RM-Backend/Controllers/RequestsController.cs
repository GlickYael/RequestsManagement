using Microsoft.AspNetCore.Mvc;
using RequestsManagement.DTOs;
using RequestsManagement.Enums;
using RequestsManagement.Services;

namespace RequestsManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestsService _requestService;

        public RequestsController(IRequestsService requestService)
        {
            _requestService = requestService;
        }

        // GET: api/requests
        [HttpGet]
        public async Task<ActionResult<RequestsPageResultDto>> GetRequests(
            [FromQuery] RequestsQueryParameters parameters,
            CancellationToken cancellationToken)
        {

            var result = await _requestService.GetPageRequestsAsync(parameters, cancellationToken);

            return Ok(result);
        }

        // PATCH: api/requests/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateStatusDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _requestService.UpdateStatusAsync(id, dto, cancellationToken);

            return result switch
            {
                UpdateStatusResult.Success => NoContent(), // 204 - success, nothing to return
                UpdateStatusResult.NotFound => NotFound(new { message = result.ToMessage() }), // 404 - request doesn't exist
                UpdateStatusResult.InvalidStatus => BadRequest(new { message = result.ToMessage() }), // 400 - invalid status
                UpdateStatusResult.Conflict => Conflict(new { message = result.ToMessage() }), // 409 - edit conflict (lost update prevented)
                _ => StatusCode(500)
            };
        }

        // PATCH: api/requests/batch-status
        [HttpPatch("batch-status")]
        public async Task<ActionResult<List<BulkUpdateFailureDto>>> BulkUpdateStatus(
            [FromBody] BulkUpdateStatusDto dto,
            CancellationToken cancellationToken)
        {
            var failures = await _requestService.BulkUpdateStatusAsync(dto, cancellationToken);

            // Returns 200 OK with the list of failures (an empty list means everything succeeded)
            return Ok(failures);
        }
    }
}