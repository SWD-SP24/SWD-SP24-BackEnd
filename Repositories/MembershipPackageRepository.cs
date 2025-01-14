using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.Repositories
{
    public class MembershipPackageRepository : IMembershipPackageRepository
    {
        private readonly AppDbContext dbContext;
        private readonly IMapper _mapper;

        public MembershipPackageRepository(AppDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<GetMembershipPackageDTO>> GetMembershipPackagesAsync()
        { 
            var membershipPackages = await dbContext.MembershipPackages
                .Include(mp => mp.Permissions)
                .ToListAsync();

            
            var membershipPackageDTOs = _mapper.Map<List<GetMembershipPackageDTO>>(membershipPackages);

            return membershipPackageDTOs;
        }
    }
}
