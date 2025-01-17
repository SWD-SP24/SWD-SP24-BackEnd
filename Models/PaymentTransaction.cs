namespace SWD392.Models
{
    public class PaymentTransaction
    {
        public int PaymentTransactionId { get; set; }
        public string PaymentId { get; set; } // Mã giao dịch thanh toán
        public int UserId { get; set; } // ID người dùng thanh toán
        public int MembershipPackageId { get; set; } // ID gói thành viên
        public decimal Amount { get; set; } // Số tiền thanh toán
        public DateTime TransactionDate { get; set; } // Thời gian giao dịch
        public string Status { get; set; } // Trạng thái giao dịch (pending, success, failed)

       

        // Navigation properties
        public User User { get; set; }
        public MembershipPackage MembershipPackage { get; set; }
    }

}
