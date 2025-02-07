using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.Repositories
{
    public class MembershipPackageRepository : IMembershipPackageRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        // Lưu ý: Không sử dụng IHttpContextAccessor trong constructor.
        public MembershipPackageRepository(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<GetMembershipPackageDTO>> GetMembershipPackagesAsync(int? userId)
        {
            IQueryable<MembershipPackage> query = _dbContext.MembershipPackages
                .Include(mp => mp.Permissions);

            if (userId.HasValue)
            {
                // Kiểm tra xem người dùng đã từng mua gói nào chưa (bất kể gói đó còn hạn hay không)
                bool hasPurchased = await _dbContext.UserMemberships
                    .AnyAsync(um => um.UserId == userId.Value);

                if (hasPurchased)
                {
                    // Nếu người dùng đã mua, loại bỏ gói dùng thử (giả sử MembershipPackageId của gói dùng thử là 4)
                    query = query.Where(mp => mp.MembershipPackageId != 4);
                }
            }

            var membershipPackages = await query.ToListAsync();
            return _mapper.Map<List<GetMembershipPackageDTO>>(membershipPackages);
        }
    }
}
