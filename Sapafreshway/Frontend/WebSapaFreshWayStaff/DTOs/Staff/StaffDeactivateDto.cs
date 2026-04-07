using System.ComponentModel.DataAnnotations;

namespace WebSapaFreshWayStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for deactivating staff
    /// </summary>
    public class StaffDeactivateDto
    {
        [Required]
        public int StaffId { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}

