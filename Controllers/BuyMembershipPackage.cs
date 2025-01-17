using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal;
using PayPal.Api;
using SWD392.Data;
using SWD392.Models;
using System.IdentityModel.Tokens.Jwt;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyMembershipPackage : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PayPalController _paypal;
        private readonly IConfiguration _configuration;
        public BuyMembershipPackage(AppDbContext context, PayPalController paypal, IConfiguration configuration)
        {
            _context = context;
            _paypal = paypal;   
            _configuration = configuration;
        }
      /*  // GET: api/<BuyMembershipPackage>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<BuyMembershipPackage>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }
*/
        // POST api/<BuyMembershipPackage>



        // POST: api/PaymentTransaction
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] int idPackage)
        {
            var authHeader = HttpContext.Request.Headers["Authorization"][0];
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(authHeader);
            var rawId = token.Claims.First(claim => claim.Type == "id").Value;
            var id = int.Parse(rawId);

            // Kiểm tra sự tồn tại của gói thành viên
            var checkPackage = await _context.MembershipPackages
                .FirstOrDefaultAsync(x => x.MembershipPackageId == idPackage);

            if (checkPackage == null)
            {
                return BadRequest(new { message = "Package not found" });
            }

            // Tạo một giao dịch thanh toán trong bảng PaymentTransactions
            var paymentTransaction = new PaymentTransaction
            {
                UserId = id,  // Gắn người dùng
                MembershipPackageId = idPackage,  // Gắn gói thành viên
                Amount = checkPackage.Price,  // Gắn giá trị gói thành viên
                TransactionDate = DateTime.UtcNow,  // Thời gian giao dịch
                Status = "pending", // Trạng thái giao dịch ban đầu là pending
            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            // Khởi tạo APIContext từ cấu hình PayPal
            var apiContext = PayPalConfiguration.GetAPIContext(_configuration);

            // Tạo đối tượng Payment để gửi thanh toán đến PayPal
            var payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                transactions = new List<Transaction>
        {
            new Transaction
            {
                amount = new Amount
                {
                    currency = "USD",
                    total = checkPackage.Price.ToString()
                },
                description = "Membership package purchase"
            }
        },
                redirect_urls = new RedirectUrls
                {
                    return_url = "https://localhost:7067/api/PayPal/execute-payment?idMbPackage=" + idPackage,
                    cancel_url = "https://localhost:7067/api/PayPal/cancel-payment"
                }
            };

            try
            {
                // Thực hiện thanh toán và nhận PaymentId từ PayPal
                var createdPayment = payment.Create(apiContext);
                var paymentId = createdPayment.id;

                // Lưu PaymentId từ PayPal vào bảng PaymentTransactions
                paymentTransaction.PaymentId = paymentId;  // Lưu paymentId của PayPal vào bảng PaymentTransactions

                // Cập nhật PaymentTransaction trong cơ sở dữ liệu
                _context.PaymentTransactions.Update(paymentTransaction);
                await _context.SaveChangesAsync();

                // Trả về liên kết thanh toán và ID giao dịch đã tạo
                var approvalUrl = createdPayment.links.FirstOrDefault(
                    link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;

                if (approvalUrl == null)
                {
                    return BadRequest(new { message = "Không tìm thấy URL phê duyệt từ PayPal." });
                }

                return Ok(new { link = approvalUrl, transactionId = paymentTransaction.PaymentTransactionId });
            }
            catch (PayPalException ex)
            {
                return BadRequest(new { message = "Có lỗi xảy ra khi thực hiện thanh toán", error = ex.Message });
            }
        }




/*
        // PUT api/<BuyMembershipPackage>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<BuyMembershipPackage>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}
