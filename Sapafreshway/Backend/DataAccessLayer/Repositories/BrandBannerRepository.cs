using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer.Repositories
{
    public class BrandBannerRepository : Repository<BrandBanner>, IBrandBannerRepository
    {
        private readonly SapaFreshContext _context;

        public BrandBannerRepository(SapaFreshContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<BrandBanner> GetActiveBanners()
        {
            return _context.BrandBanners
                .Include(b => b.CreatedByNavigation) 
                .Where(b => b.Status == "Active")
                .ToList();
        }
        public IEnumerable<BrandBanner> GetAllWithUser()
        {
            return _context.BrandBanners
                .Include(b => b.CreatedByNavigation) 
                .ToList();
        }
    }
}
