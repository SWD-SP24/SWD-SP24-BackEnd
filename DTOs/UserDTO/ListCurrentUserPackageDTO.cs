using SWD392.DTOs.MembershipPackagesDTO;

namespace SWD392.DTOs.UserDTO
{
    public class ListCurrentUserPackageDTO
    {
        public int UserId { get; set; }

        public required string Email { get; set; }
        public required string FullName { get; set; }
        public int? MembershipPackageId { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public virtual GetCurrentPackageAllUserDTO MembershipPackage { get; set; }
    }
}
