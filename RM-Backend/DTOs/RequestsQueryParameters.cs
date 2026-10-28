using System.ComponentModel.DataAnnotations;
using RequestsManagement.Enums;

namespace RequestsManagement.DTOs
{
    public class RequestsQueryParameters : IValidatableObject
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than or equal to 1.")]
        public int PageNumber { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100.")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
        }

        [StringLength(50, ErrorMessage = "Organization filter is too long.")]
        public string? Organization { get; set; }

        [StringLength(20, ErrorMessage = "Status filter is too long.")]
        [EnumDataType(typeof(RequestStatus), ErrorMessage = "Invalid status filter value.")]
        public string? Status { get; set; }

        [StringLength(20, ErrorMessage = "Priority filter is too long.")]
        [EnumDataType(typeof(RequestPriority), ErrorMessage = "Invalid priority filter value.")]
        public string? Priority { get; set; }

        [StringLength(50, ErrorMessage = "AssignedTo filter is too long.")]
        public string? AssignedTo { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters.")]
        public string? SearchTerm { get; set; }

        [RegularExpression("^(CreatedAt|Title|Priority)$",
            ErrorMessage = "Invalid sort field. Allowed fields are: CreatedAt, Title, Priority.")]
        public string SortBy { get; set; } = "CreatedAt";

        public bool SortDescending { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FromDate.HasValue && ToDate.HasValue && FromDate.Value > ToDate.Value)
            {
                yield return new ValidationResult(
                    "FromDate cannot be later than ToDate.",
                    new[] { nameof(FromDate), nameof(ToDate) });
            }
        }
    }
}
