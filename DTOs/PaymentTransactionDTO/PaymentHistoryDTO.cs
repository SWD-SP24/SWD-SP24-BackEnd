namespace SWD392.DTOs.PaymentTransactionDTO
{
    public class PaymentHistoryDTO
    {
        public string PaymentId { get; set; }
        public int UserId { get; set; }
        public int MembershipPackageId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
