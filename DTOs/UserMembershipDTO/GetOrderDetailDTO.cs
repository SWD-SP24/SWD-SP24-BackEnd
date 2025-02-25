using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.DTOs.UserMembershipDTO
{
    public class GetOrderDetailDTO
    {
        public int MembershipPackageId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? PaymentTransactionId { get; set; }

        public virtual GetMembershipPackageDTO MembershipPackage { get; set; }
    }
}
