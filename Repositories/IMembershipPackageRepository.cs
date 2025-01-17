using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.Repositories
{
    public interface IMembershipPackageRepository
    {
        Task<List<GetMembershipPackageDTO>> GetMembershipPackagesAsync();
    }
}
