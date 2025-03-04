using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal;
using PayPal.Api;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.DTOs.PaymentTransactionDTO;
using SWD392.DTOs.UserMembershipDTO;
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


        [HttpGet("{idPackage}")]
        public async Task<IActionResult> GetOrderDetail(int idPackage)
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
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(x => x.MembershipPackageId == idPackage);

            if (requestedPackage == null)
            {
                return BadRequest(new { message = "Package not found" });
            }

            var currentMembership = await _context.UserMemberships
                .FirstOrDefaultAsync(um => um.UserId == userId && um.EndDate > DateTime.UtcNow);

            decimal remainingPrice = 0;
            int remainingDays = 0;
            int additionalDays = 0;

            if (currentMembership != null)
            {
                var currentPackage = await _context.MembershipPackages
                    .FirstOrDefaultAsync(x => x.MembershipPackageId == currentMembership.MembershipPackageId);

                if (currentPackage != null && requestedPackage.Price < currentPackage.Price && requestedPackage.Price == 0)
                {
                    return BadRequest(new { message = "Bạn không thể mua gói thấp hơn gói hiện tại." });
                }

                // Calculate the remaining days based on the current membership's validity
                var remainingTime = currentMembership.EndDate - DateTime.UtcNow;

                // Check if the remainingTime has a value and access TotalDays
                remainingDays = remainingTime.HasValue ? (int)remainingTime.Value.TotalDays : 0;

                // Calculate additional days based on the new package's price and old package's remaining days
                additionalDays = (int)((remainingDays * requestedPackage.Price) / currentPackage.Price);

                // Ensure the additional days are non-negative
                additionalDays = Math.Max(additionalDays, 0);

                // Adjust the start date and end date for the new package
                var startDate = DateTime.UtcNow;
                var validityPeriod = requestedPackage.ValidityPeriod + additionalDays; // Add the additional days to the new package
                var endDate = startDate.AddDays(validityPeriod);




                // Ensure the price doesn't go negative
                if (requestedPackage.Price < 0)
                {
                    requestedPackage.Price = 0;
                }
            }
            else
            {
                var startDate = DateTime.UtcNow;
                var validityPeriod = requestedPackage.ValidityPeriod;
                var endDate = startDate.AddDays(validityPeriod);
            }

            var orderDetail = new GetOrderDetailDTO
            {
                MembershipPackageId = idPackage,
                StartDate = DateTime.UtcNow,  // Start date of the new package
                EndDate = DateTime.UtcNow.AddDays(requestedPackage.ValidityPeriod + additionalDays), // End date of the new package
                RemainingPrice = remainingPrice,  // Add remaining price from the old package
                RemainingDays = remainingDays,  // Add remaining days from the old package
                AdditionalDays = additionalDays, // Add the additional days based on the new package's price
                MembershipPackage = new GetMembershipPackageDTO
                {
                    MembershipPackageId = requestedPackage.MembershipPackageId,
                    MembershipPackageName = requestedPackage.MembershipPackageName,
                    Price = requestedPackage.Price,  // Updated price after applying the remaining money
                    Status = requestedPackage.Status,
                    ValidityPeriod = requestedPackage.ValidityPeriod,
                    Permissions = requestedPackage.Permissions.Select(p => new PermissionDTO
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        Description = p.Description
                    }).ToList()
                }
            };

            return Ok(orderDetail);
        }




        [HttpPost("BuyMembershipPackage")]
        public async Task<IActionResult> BuyMembership([FromBody] BuyMembershipRequest request)
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

            // Lấy gói đăng ký cần mua
            var requestedPackage = await _context.MembershipPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MembershipPackageId == request.IdPackage);

            if (requestedPackage == null)
            {
                return BadRequest(new { message = "Package not found" });
            }

            // Xác định giá gói theo loại thanh toán
            int validityDays = request.PaymentType == "yearly" ? 365 : requestedPackage.ValidityPeriod;
            decimal packagePrice = request.PaymentType == "yearly" ? requestedPackage.YearlyPrice : requestedPackage.Price;

            // Kiểm tra membership hiện tại của user
            var currentMembership = await _context.UserMemberships
                .Where(um => um.UserId == userId && um.EndDate > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            string previousPackageName = null;
            if (currentMembership != null)
            {
                var currentPackage = await _context.MembershipPackages
                    .Where(x => x.MembershipPackageId == currentMembership.MembershipPackageId)
                    .FirstOrDefaultAsync();

                if (packagePrice > (request.PaymentType == "yearly" ? currentPackage.YearlyPrice : currentPackage.Price))
                {
                    previousPackageName = currentPackage.MembershipPackageName;

                    // Tính số ngày còn dư của gói cũ
                    DateTime now = DateTime.UtcNow;
                    TimeSpan remainingTime = (currentMembership.EndDate ?? now) - now; // Chuyển từ nullable thành TimeSpan
                    int remainingDays = Math.Max(0, remainingTime.Days); // Đảm bảo số ngày không âm

                    if (remainingDays > 0)
                    {
                        // Giá của gói cũ
                        decimal currentPackagePrice = request.PaymentType == "yearly" ? currentPackage.YearlyPrice : currentPackage.Price;
                        int currentPackageDays = request.PaymentType == "yearly" ? 365 : currentPackage.ValidityPeriod;

                        // Giá trị mỗi ngày của gói cũ
                        decimal dailyRateOld = currentPackagePrice / currentPackageDays;

                        // Tổng giá trị còn lại
                        decimal remainingValue = remainingDays * dailyRateOld;

                        // Giá trị mỗi ngày của gói mới
                        decimal dailyRateNew = packagePrice / validityDays;

                        // Số ngày cộng vào gói mới
                        int extraDays = (int)(remainingValue / dailyRateNew);

                        // Cộng thêm số ngày vào thời hạn gói mới
                        validityDays += extraDays;
                    }
                }
            }

            

            
            // Tạo transaction thanh toán
            var paymentTransaction = new PaymentTransaction
            {
                UserId = userId,
                MembershipPackageId = request.IdPackage,
                Amount = packagePrice,
                TransactionDate = DateTime.UtcNow,
                Status = "pending",
                PreviousMembershipPackageName = previousPackageName,
            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            // Tạo UserMembership với status là "pending"
            
            // Tạo thanh toán PayPal
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
                    total = packagePrice.ToString("F2")
                },
                description = $"Purchase {request.PaymentType} membership package"
            }
        },
                redirect_urls = new RedirectUrls
                {
                    return_url = $"https://localhost:7067/api/PayPal/execute-payment?idMbPackage={request.IdPackage}&paymentType={request.PaymentType}&validityDays={validityDays}",
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
