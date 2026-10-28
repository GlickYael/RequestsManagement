using System.ComponentModel.DataAnnotations;

namespace RequestsManagement.Models
{
    public class RequestStatusAuditEntity
    {
        public int Id { get; set; }

        public int RequestId { get; set; }

        public RequestEntity Request { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string PreviousStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string NewStatus { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string ChangedBy { get; set; } = "System";
    }
}