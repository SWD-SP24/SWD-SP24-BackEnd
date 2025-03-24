using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.DTOs.PaymentTransactionDTO
{
    public class PaymentHistoryDTO
    {

        public int PaymentTransactionId { get; set; }

        public string PaymentId { get; set; }
        public int UserId { get; set; }
        public string PreviousMembershipPackageName { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public virtual GetPackageUserHistoryDTO MembershipPackage { get; set; }
    }
}
