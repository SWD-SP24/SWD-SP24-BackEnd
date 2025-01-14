using SWD392.DTOs.MembershipPackagesDTO;

namespace SWD392.Repositories
{
    public interface IMembershipPackageRepository
    {
        Task<List<GetMembershipPackageDTO>> GetMembershipPackagesAsync();
    }
}
