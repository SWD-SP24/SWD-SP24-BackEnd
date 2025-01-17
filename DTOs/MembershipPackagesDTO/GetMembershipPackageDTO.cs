using SWD392.Models;

namespace SWD392.DTOs.MembershipPackagesDTO
{
    public class GetMembershipPackageDTO
    {
        public int MembershipPackageId { get; set; }

        public string MembershipPackageName { get; set; }

        public decimal Price { get; set; }
        public string Status { get; set; }

        public int ValidityPeriod { get; set; }
        public virtual ICollection<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }
}
