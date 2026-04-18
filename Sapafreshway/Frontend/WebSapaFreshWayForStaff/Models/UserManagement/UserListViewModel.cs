using SapaFreshWayForStaff.DTOs;
using SapaFreshWayForStaff.DTOs.UserManagement;
using DTOsRole = SapaFreshWayForStaff.DTOs.Role;

namespace SapaFreshWayForStaff.Models.UserManagement
{
    public class UserListViewModel
    {
        public UserListResponse? UserList { get; set; }
        public List<DTOsRole>? AvailableRoles { get; set; }
        public UserSearchRequest? SearchRequest { get; set; }
    }
}

