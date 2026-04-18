using SapaFreshWayForStaff.DTOs.Staff;
using SapaFreshWayForStaff.Services.Api.Interfaces;

namespace SapaFreshWayForStaff.Models.StaffViewModels
{
    /// <summary>
    /// ViewModel for Staff Index page
    /// </summary>
    public class StaffIndexViewModel
    {
        /// <summary>
        /// Filter criteria
        /// </summary>
        public StaffFilterDto Filter { get; set; } = new StaffFilterDto();

        /// <summary>
        /// Staff list data
        /// </summary>
        public StaffListResponse? StaffList { get; set; }

        /// <summary>
        /// Available positions for filter dropdown
        /// </summary>
        public List<PositionDto> AvailablePositions { get; set; } = new();
    }
}

