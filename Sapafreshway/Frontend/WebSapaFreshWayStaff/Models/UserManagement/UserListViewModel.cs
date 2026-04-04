using WebSapaFreshWayStaff.DTOs;
using WebSapaFreshWayStaff.DTOs.UserManagement;
using DTOsRole = WebSapaFreshWayStaff.DTOs.Role;

namespace WebSapaFreshWayStaff.Models.UserManagement
{
    public class UserListViewModel
    {
        public UserListResponse? UserList { get; set; }
        public List<DTOsRole>? AvailableRoles { get; set; }
        public UserSearchRequest? SearchRequest { get; set; }
    }
}

