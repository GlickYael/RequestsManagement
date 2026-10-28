using RequestsManagement.Enums;
using System.ComponentModel.DataAnnotations;

namespace RequestsManagement.DTOs
{
    public class BulkUpdateStatusDto
    {
        [Required(ErrorMessage = "Target status is required.")]
        [StringLength(20, ErrorMessage = "Status is too long.")]
        [EnumDataType(typeof(RequestStatus), ErrorMessage = "Invalid target status value.")]
        public string Status { get; set; } = string.Empty; // the new status all items are being changed to

        [Required]
        [MaxLength(100, ErrorMessage = "Cannot update more than 100 requests at once.")]
        public List<BulkUpdateItemDto> Items { get; set; } = new();
    }

    public class BulkUpdateItemDto
    {
        public int Id { get; set; }

        public int Version { get; set; }

        [Required]
        [StringLength(20)]
        public string OldStatus { get; set; } = string.Empty; // the old status as sent by the client
    }

    public class BulkUpdateFailureDto
    {
        public int Id { get; set; }
        public UpdateStatusResult ErrorReason { get; set; } // failure type, from the Enum
        public string Message { get; set; } = string.Empty;   // human-readable error message
    }
}