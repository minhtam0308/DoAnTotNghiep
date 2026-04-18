using SapaFreshWayForStaff.DTOs.CustomerManagement;

namespace SapaFreshWayForStaff.ViewModels.CustomerManagement
{
    /// <summary>
    /// ViewModel for customer list page
    /// </summary>
    public class CustomerListViewModel
    {
        public List<CustomerListItemDto> Items { get; set; } = new();
        public CustomerFilterViewModel Filters { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
    }
}

