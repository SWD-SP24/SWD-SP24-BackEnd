namespace SWD392.DTOs.PaymentTransactionDTO
{
    public class PendingPaymentDTO
    {
        public int PaymentTransactionId { get; set; }

        public string PaymentId { get; set; }

        public int UserId { get; set; }

        public int MembershipPackageId { get; set; }

        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }

        public string Status { get; set; }

        public string PreviousMembershipPackageName { get; set; }

        public int? UserMembershipId { get; set; }

        public string PaymentLink { get; set; }
        public string PaymentType { get; set; }
    }
}
