using System.IdentityModel.Tokens.Jwt;

namespace SWD392.DTOs.PaymentTransactionDTO
{

    public class BuyMembershipRequest
    {
        public int IdPackage { get; set; }
        public string PaymentType { get; set; } // "monthly" hoặc "yearly"
    }

   


}
