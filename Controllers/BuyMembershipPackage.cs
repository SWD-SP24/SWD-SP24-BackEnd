using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal;
using PayPal.Api;
using SWD392.Data;
using SWD392.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

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



        [HttpPost]
        public async Task<IActionResult> Post([FromBody] int idPackage)
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new { message = "Authorization header missing" });
            }

            var handler = new JwtSecurityTokenHandler();
            var header = AuthenticationHeaderValue.Parse(authHeader);
            var token = handler.ReadJwtToken(header.Parameter);
            var rawId = token.Claims.FirstOrDefault(claim => claim.Type == "id")?.Value;
            if (string.IsNullOrEmpty(rawId) || !int.TryParse(rawId, out int userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var requestedPackage = await _context.MembershipPackages
                .FirstOrDefaultAsync(x => x.MembershipPackageId == idPackage);
            if (requestedPackage == null)
            {
                return BadRequest(new { message = "Package not found" });
            }

            var currentMembership = await _context.UserMemberships
                .FirstOrDefaultAsync(um => um.UserId == userId && um.EndDate > DateTime.UtcNow);

            if (currentMembership != null)
            {
                var currentPackage = await _context.MembershipPackages
                    .FirstOrDefaultAsync(x => x.MembershipPackageId == currentMembership.MembershipPackageId);

                if (currentPackage != null && requestedPackage.Price < currentPackage.Price)
                {
                    return BadRequest(new { message = "Bạn không thể mua gói có giá thấp hơn gói hiện tại." });
                }
            }


            var paymentTransaction = new PaymentTransaction
            {
                UserId = userId,
                MembershipPackageId = idPackage,
                Amount = requestedPackage.Price,
                TransactionDate = DateTime.UtcNow,
                Status = "pending"
            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            var apiContext = PayPalConfiguration.GetAPIContext(_configuration);

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
                    total = requestedPackage.Price.ToString("F2")
                },
                description = "Membership package purchase"
            }
        },
                redirect_urls = new RedirectUrls
                {
                    return_url = $"https://localhost:7067/api/PayPal/execute-payment?idMbPackage={idPackage}",
                    cancel_url = "https://localhost:7067/api/PayPal/cancel-payment"
                }
            };

            try
            {
                var createdPayment = payment.Create(apiContext);
                var paymentId = createdPayment.id;

                paymentTransaction.PaymentId = paymentId;
                _context.PaymentTransactions.Update(paymentTransaction);
                await _context.SaveChangesAsync();

                var approvalUrl = createdPayment.links
                    .FirstOrDefault(link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;

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

        [HttpPost("auto-purchase-free-package")]
        public async Task<IActionResult> AutoPurchaseFreePackage(string id)
        {
            // Lấy Authorization header và kiểm tra token
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new { message = "Authorization header missing" });
            }

            var rawId = id;
            if (string.IsNullOrEmpty(rawId) || !int.TryParse(rawId, out int userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Xác định gói miễn phí (điều kiện: gói có id = 1)
            int idPackage = 1;
            var requestedPackage = await _context.MembershipPackages
                .FirstOrDefaultAsync(x => x.MembershipPackageId == idPackage);
            if (requestedPackage == null)
            {
                return BadRequest(new { message = "Package not found" });
            }
            // Kiểm tra nếu gói này không miễn phí thì không xử lý qua API này
            if (requestedPackage.Price != 0)
            {
                return BadRequest(new { message = "Gói này không miễn phí." });
            }

            // Kiểm tra xem người dùng đã có membership active hay chưa
            var currentMembership = await _context.UserMemberships
                .FirstOrDefaultAsync(um => um.UserId == userId && um.EndDate > DateTime.UtcNow);

            // Nếu đã có gói hiện tại và gói mới (id=1) có thứ tự thấp hơn gói hiện tại thì không cho mua
            if (currentMembership != null && idPackage < currentMembership.MembershipPackageId)
            {
                return BadRequest(new { message = "Bạn không thể mua gói thấp hơn gói hiện tại." });
            }

            // Tạo một PaymentTransaction mới với Amount = 0 và Status là "success" (vì không cần thanh toán)
            var paymentTransaction = new PaymentTransaction
            {
                UserId = userId,
                MembershipPackageId = idPackage,
                Amount = requestedPackage.Price,
                TransactionDate = DateTime.UtcNow,
                Status = "success",  // tự động thành công
                PaymentId = "FREE"   // đánh dấu là giao dịch miễn phí
            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            // Xử lý việc gia hạn hoặc tạo mới UserMembership dựa trên tình trạng hiện tại
            if (currentMembership != null)
            {
                if (currentMembership.MembershipPackageId == idPackage)
                {
                    // Gia hạn gói hiện tại: cộng thêm số ngày theo ValidityPeriod
                    currentMembership.EndDate = currentMembership.EndDate.Value.AddDays(requestedPackage.ValidityPeriod);
                    _context.UserMemberships.Update(currentMembership);
                }
                else
                {
                    // Trường hợp nâng cấp hoặc chuyển gói:
                    // Đánh dấu gói hiện tại hết hạn và tạo mới gói đăng ký
                    currentMembership.EndDate = DateTime.UtcNow;
                    currentMembership.Status = "expired";
                    _context.UserMemberships.Update(currentMembership);

                    var newMembership = new UserMembership
                    {
                        UserId = userId,
                        MembershipPackageId = idPackage,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(requestedPackage.ValidityPeriod),
                        Status = "active",
                        PaymentTransactionId = paymentTransaction.PaymentTransactionId
                    };
                    _context.UserMemberships.Add(newMembership);
                }
            }
            else
            {
                // Nếu người dùng chưa có membership active thì tạo mới
                var newMembership = new UserMembership
                {
                    UserId = userId,
                    MembershipPackageId = idPackage,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(requestedPackage.ValidityPeriod),
                    Status = "active",
                    PaymentTransactionId = paymentTransaction.PaymentTransactionId
                };
                _context.UserMemberships.Add(newMembership);
            }

            // Cập nhật MembershipPackageId cho người dùng
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.MembershipPackageId = idPackage;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Mua gói miễn phí thành công", transactionId = paymentTransaction.PaymentTransactionId });
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
