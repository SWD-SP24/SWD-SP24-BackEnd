using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.DTOs.UserMembershipDTO
{
    public class GetOrderDetailDTO
    {
        public int MembershipPackageId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public decimal RemainingPrice { get; set; }  
        public int RemainingDays { get; set; }   
        public int AdditionalDays { get; set; }
        public virtual OrderDetail2DTO MembershipPackage { get; set; }
    }
}
