using System.ComponentModel.DataAnnotations;
using RequestsManagement.Enums;

namespace RequestsManagement.DTOs
{
    public class UpdateStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20, ErrorMessage = "Status is too long.")]
        [EnumDataType(typeof(RequestStatus), ErrorMessage = "Invalid status value.")]
        public string Status { get; set; } = string.Empty;

        public int Version { get; set; }
    }
}
