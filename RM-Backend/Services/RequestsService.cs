using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using RequestsManagement.Data;
using RequestsManagement.DTOs;
using RequestsManagement.Enums;
using RequestsManagement.Models;

namespace RequestsManagement.Services
{
    public class RequestsService : IRequestsService
    {
        private readonly AppDbContext _context;
        private readonly IRequestStatusAuditService _auditService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RequestsService> _logger;

        // Generation token for the aggregation cache: every successful write cancels it,
        // which invalidates all existing cache entries at once (see InvalidateAggregationCache).
        private static CancellationTokenSource _cacheInvalidationTokenSource = new();

        public RequestsService(
            AppDbContext context,
            IRequestStatusAuditService auditService,
            IMemoryCache cache,
            ILogger<RequestsService> logger)
        {
            _context = context;
            _auditService = auditService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<RequestsPageResultDto> GetPageRequestsAsync(RequestsQueryParameters parameters, CancellationToken cancellationToken)
        {
            var query = _context.Requests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.Organization))
            {
                query = query.Where(r => r.OrganizationName.Contains(parameters.Organization));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Status) && Enum.TryParse<RequestStatus>(parameters.Status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Priority) && Enum.TryParse<RequestPriority>(parameters.Priority, true, out var parsedPriority))
            {
                query = query.Where(r => r.Priority == parsedPriority);
            }

            if (!string.IsNullOrWhiteSpace(parameters.AssignedTo))
            {
                query = query.Where(r => r.AssignedTo != null && r.AssignedTo.Contains(parameters.AssignedTo));
            }

            if (parameters.FromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= parameters.FromDate.Value);
            }

            if (parameters.ToDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= parameters.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var term = parameters.SearchTerm.Trim();
                query = query.Where(r => r.Title.Contains(term) || r.OrganizationName.Contains(term));
            }

            var (statusCounts, priorityCounts) = await GetAggregationsAsync(parameters, query, cancellationToken);

            int totalCount = await query.CountAsync(cancellationToken);

            query = parameters.SortBy.ToLower() switch
            {
                "title" => parameters.SortDescending
                    ? query.OrderByDescending(r => r.Title)
                    : query.OrderBy(r => r.Title),

                "priority" => parameters.SortDescending
                    ? query.OrderByDescending(r => r.Priority)
                    : query.OrderBy(r => r.Priority),

                _ => parameters.SortDescending
                    ? query.OrderByDescending(r => r.CreatedAt)
                    : query.OrderBy(r => r.CreatedAt)
            };

            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(r => new RequestDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    OrganizationName = r.OrganizationName,
                    Status = r.Status.ToString(),
                    Priority = r.Priority.ToString(),
                    AssignedTo = r.AssignedTo ?? string.Empty,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Version = r.Version
                })
                .ToListAsync(cancellationToken);

            return new RequestsPageResultDto
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                StatusCounts = statusCounts,
                PriorityCounts = priorityCounts
            };
        }

        /// <summary>
        /// StatusCounts/PriorityCounts are a relatively expensive summary (GroupBy over the
        /// whole filtered set) that doesn't need sub-second accuracy, so they're cached for
        /// 30 seconds keyed by the filter parameters relevant to the aggregation (excluding
        /// Sort/Page, which don't affect it). Invalidated on every successful write.
        /// </summary>
        private async Task<(Dictionary<string, int> StatusCounts, Dictionary<string, int> PriorityCounts)> GetAggregationsAsync(
            RequestsQueryParameters parameters,
            IQueryable<RequestEntity> filteredQuery,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"aggregations:{parameters.Organization}|{parameters.Status}|{parameters.Priority}|{parameters.AssignedTo}|{parameters.FromDate:O}|{parameters.ToDate:O}|{parameters.SearchTerm}";

            if (_cache.TryGetValue(cacheKey, out (Dictionary<string, int> StatusCounts, Dictionary<string, int> PriorityCounts) cached))
            {
                return cached;
            }

            var statusCounts = await filteredQuery
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

            var priorityCounts = await filteredQuery
                .GroupBy(r => r.Priority)
                .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Priority, x => x.Count, cancellationToken);

            var result = (statusCounts, priorityCounts);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                .AddExpirationToken(new CancellationChangeToken(_cacheInvalidationTokenSource.Token));

            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        /// <summary>
        /// Invalidates all aggregation cache entries at once, regardless of the filter
        /// combination they were stored under. Scale-out note: IMemoryCache is per-instance
        /// (in-process) - with multiple server instances this only affects the current
        /// instance; see README for a discussion of the IDistributedCache alternative.
        /// </summary>
        private static void InvalidateAggregationCache()
        {
            var previous = Interlocked.Exchange(ref _cacheInvalidationTokenSource, new CancellationTokenSource());
            previous.Cancel();
            previous.Dispose();
        }

        public async Task<UpdateStatusResult> UpdateStatusAsync(int id, UpdateStatusDto dto, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<RequestStatus>(dto.Status, true, out var parsedStatus))
            {
                return UpdateStatusResult.InvalidStatus;
            }

            var request = await _context.Requests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (request == null)
            {
                return UpdateStatusResult.NotFound;
            }

            var (result, changed) = await ApplyStatusUpdateAsync(request, parsedStatus, dto.Version, cancellationToken);
            if (result != UpdateStatusResult.Success)
            {
                if (result == UpdateStatusResult.Conflict)
                {
                    _logger.LogWarning(
                        "Concurrency conflict updating request {RequestId}: client version {ClientVersion} vs current {CurrentVersion}",
                        id, dto.Version, request.Version);
                }
                return result;
            }

            // Same status as today - nothing to write, and no reason to invalidate the cache
            if (!changed)
            {
                return UpdateStatusResult.Success;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning(
                    "Concurrency conflict (DB-level) updating request {RequestId}: client version {ClientVersion}",
                    id, dto.Version);
                return UpdateStatusResult.Conflict;
            }

            InvalidateAggregationCache();
            _logger.LogInformation("Request {RequestId} status updated to {NewStatus} (version {Version})", id, parsedStatus, request.Version);

            return UpdateStatusResult.Success;
        }

        public async Task<List<BulkUpdateFailureDto>> BulkUpdateStatusAsync(BulkUpdateStatusDto dto, CancellationToken cancellationToken)
        {
            var failures = new List<BulkUpdateFailureDto>();

            if (!Enum.TryParse<RequestStatus>(dto.Status, true, out var parsedStatus))
            {
                foreach (var item in dto.Items)
                {
                    failures.Add(NewFailure(item.Id, UpdateStatusResult.InvalidStatus));
                }
                return failures;
            }

            // One query for all items instead of one per item
            var ids = dto.Items.Select(i => i.Id).Distinct().ToList();
            var requestsById = await _context.Requests
                .Where(r => ids.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, cancellationToken);

            var anyChanged = false;

            foreach (var item in dto.Items)
            {
                if (!requestsById.TryGetValue(item.Id, out var request))
                {
                    failures.Add(NewFailure(item.Id, UpdateStatusResult.NotFound));
                    continue;
                }

                // Check the client-supplied "old status" against what's actually in the DB -
                // a mismatch here means the client's data is stale, which is a Conflict, not invalid input
                if (!request.Status.ToString().Equals(item.OldStatus, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add(new BulkUpdateFailureDto
                    {
                        Id = item.Id,
                        ErrorReason = UpdateStatusResult.Conflict,
                        Message = "The provided old status does not match the current database status (stale data)."
                    });
                    continue;
                }

                var (result, changed) = await ApplyStatusUpdateAsync(request, parsedStatus, item.Version, cancellationToken);
                if (result != UpdateStatusResult.Success)
                {
                    failures.Add(NewFailure(item.Id, result));
                    continue;
                }

                if (!changed)
                {
                    continue;
                }

                // Saved per item on purpose: a DB-level conflict on one item must not roll back
                // the others (Partial Success). The reads are batched above; the writes are not.
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    anyChanged = true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Drop this item's pending changes (request + audit row), otherwise the next
                    // SaveChanges in the loop would retry them and fail every following item too.
                    DiscardPendingChanges();
                    failures.Add(NewFailure(item.Id, UpdateStatusResult.Conflict));
                }
            }

            if (anyChanged)
            {
                InvalidateAggregationCache();
            }

            if (failures.Count == 0)
            {
                _logger.LogInformation(
                    "Bulk status update to {TargetStatus}: all {TotalCount} items succeeded",
                    dto.Status, dto.Items.Count);
            }
            else
            {
                _logger.LogWarning(
                    "Bulk status update to {TargetStatus}: {FailureCount} of {TotalCount} items failed",
                    dto.Status, failures.Count, dto.Items.Count);
            }

            return failures;
        }

        private static BulkUpdateFailureDto NewFailure(int id, UpdateStatusResult reason) => new()
        {
            Id = id,
            ErrorReason = reason,
            Message = reason.ToMessage()
        };

        private void DiscardPendingChanges()
        {
            var pending = _context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var entry in pending)
            {
                entry.State = EntityState.Detached;
            }
        }

        /// <summary>
        /// The allowed status-transition matrix. A transition not listed here (including going
        /// back to New, or any transition out of Completed) is rejected with UpdateStatusResult.InvalidStatus.
        /// </summary>
        private static readonly Dictionary<RequestStatus, HashSet<RequestStatus>> AllowedTransitions = new()
        {
            [RequestStatus.New] = new HashSet<RequestStatus> { RequestStatus.InProgress },
            [RequestStatus.InProgress] = new HashSet<RequestStatus> { RequestStatus.Waiting, RequestStatus.Completed },
            [RequestStatus.Waiting] = new HashSet<RequestStatus> { RequestStatus.InProgress, RequestStatus.Completed },
            [RequestStatus.Completed] = new HashSet<RequestStatus>() // terminal status - no transitions out of it
        };

        /// <summary>
        /// Shared helper that performs the concurrency check, validates the status transition,
        /// bumps the version, updates the timestamp, and records the audit entry.
        /// Changed is false when the requested status equals the current one (a valid no-op).
        /// </summary>
        private async Task<(UpdateStatusResult Result, bool Changed)> ApplyStatusUpdateAsync(
            RequestEntity request,
            RequestStatus parsedStatus,
            int clientVersion,
            CancellationToken cancellationToken)
        {
            if (request.Version != clientVersion)
            {
                return (UpdateStatusResult.Conflict, false);
            }

            if (request.Status == parsedStatus)
            {
                return (UpdateStatusResult.Success, false);
            }

            if (!AllowedTransitions.TryGetValue(request.Status, out var allowedTargets) ||
                !allowedTargets.Contains(parsedStatus))
            {
                return (UpdateStatusResult.InvalidStatus, false);
            }

            var previousStatusString = request.Status.ToString();

            request.Status = parsedStatus;
            request.Version++;
            request.UpdatedAt = DateTime.UtcNow;

            await _auditService.AddAuditAsync(request.Id, previousStatusString, parsedStatus.ToString(), cancellationToken);

            return (UpdateStatusResult.Success, true);
        }
    }
}
