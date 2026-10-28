using System.ComponentModel.DataAnnotations;
using RequestsManagement.Enums;

namespace RequestsManagement.Models
{
    public class RequestEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.New;

        [Required]
        public RequestPriority Priority { get; set; }

        [MaxLength(100)]
        public string? AssignedTo { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int Version { get; set; } = 1;
    }
}
