using System.ComponentModel.DataAnnotations;

namespace RequestsManagement.DTOs
{
    public class RequestStatusAuditDto
    {
        public int Id { get; set; }

        [Required]
        public int RequestId { get; set; }

        [Required]
        [StringLength(20)]
        public string PreviousStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string NewStatus { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; }

        [StringLength(100)]
        public string ChangedBy { get; set; } = string.Empty;
    }
}