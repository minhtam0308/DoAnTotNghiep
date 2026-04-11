using WebSapaFreshWayStaff.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaFreshWayStaff.DTOs
{
    public class ManagerCategoryDTO
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;
    }
}
