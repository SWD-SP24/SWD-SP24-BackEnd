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
        public async Task<IActionResult> GetOrderDetail(int idPackage, [FromQuery] string paymentType)
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

            // Nếu gói 0 đồng, bỏ qua luôn
            if (requestedPackage.Price == 0 && requestedPackage.YearlyPrice == 0)
            {
                return BadRequest(new { message = "Gói miễn phí không thể đặt hàng." });
            }

            int validityPeriod = (paymentType.ToLower() == "yearly") ? 365 : requestedPackage.ValidityPeriod;

            var currentMembership = await _context.UserMemberships
                .FirstOrDefaultAsync(um => um.UserId == userId && um.EndDate > DateTime.UtcNow && um.Status == "active");

            decimal remainingPrice = 0;
            int remainingDays = 0;
            int additionalDays = 0;
            string PreviousMembershipPackageName = string.Empty;
            decimal currentPrice = 0;

            MembershipPackage currentPackage = null;

            if (currentMembership != null)
            {
                currentPackage = await _context.MembershipPackages
                    .Include(p => p.Permissions)
                    .FirstOrDefaultAsync(x => x.MembershipPackageId == currentMembership.MembershipPackageId);

                if (currentPackage != null)
                {
                    var paymentTransactionId = currentMembership?.PaymentTransactionId;

                    if (paymentTransactionId != null)
                    {
                        var transaction = await _context.PaymentTransactions
                            .FirstOrDefaultAsync(t => t.PaymentTransactionId == paymentTransactionId);

                        if (transaction != null)
                        {
                            currentPrice = transaction.Amount;
                        }
                    }
                    if (currentPrice > 0)
                    {
                        decimal requestedPrice = paymentType.ToLower() == "yearly" ? requestedPackage.YearlyPrice : requestedPackage.Price;
                        decimal currentMonthlyPrice = currentPackage.Price;
                        decimal requestedMonthlyPrice = requestedPackage.Price;
                        if (requestedMonthlyPrice < currentMonthlyPrice)
                        {
                            return BadRequest(new { message = "Bạn không thể mua gói thấp hơn gói hiện tại." });
                        }

                        var remainingTime = currentMembership.EndDate - DateTime.UtcNow;
                        remainingDays = remainingTime.HasValue ? (int)remainingTime.Value.TotalDays : 0;

                        additionalDays = requestedPrice > 0 ? (int)((remainingDays * currentMonthlyPrice) / requestedMonthlyPrice) : 0;

                        if (remainingDays > 0 && currentPrice > 0)
                        {
                            decimal pricePerDay = currentPrice > 100 ? currentPrice / 365 : currentPrice / 30;
                            remainingPrice = Math.Round(pricePerDay * remainingDays, 2);
                        }

                        additionalDays = Math.Max(additionalDays, 0);
                    }
                }
            }

            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(validityPeriod + additionalDays);

            var orderDetail = new GetOrderDetailDTO
            {
                MembershipPackageId = idPackage,
                StartDate = startDate,
                EndDate = endDate,
                PreviousMembershipPackageName = PreviousMembershipPackageName,
                RemainingPrice = remainingPrice,
                RemainingDays = remainingDays,
                AdditionalDays = additionalDays,
                MembershipPackage = new OrderDetail2DTO
                {
                    MembershipPackageId = requestedPackage.MembershipPackageId,
                    MembershipPackageName = requestedPackage.MembershipPackageName,
                    Price = requestedPackage.Price,
                    YearlyPrice = requestedPackage.YearlyPrice,
                    PercentDiscount = requestedPackage.Price > 0
                        ? (int)Math.Round(100 - ((requestedPackage.YearlyPrice / (requestedPackage.Price * 12)) * 100), 2)
                        : 0,
                    Status = requestedPackage.Status,
                    ValidityPeriod = validityPeriod,
                    SavingPerMonth = Math.Round(requestedPackage.Price > 0
                        ? requestedPackage.Price - (requestedPackage.YearlyPrice / 12)
                        : 0, 2),
                    Image = requestedPackage.Image,
                    Summary = requestedPackage.Summary,
                    Permissions = requestedPackage.Permissions.Select(p => new PermissionDTO
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        Description = p.Description
                    }).ToList()
                },
                CurrentMembershipPackage = currentPackage != null ? new OrderDetail2DTO
                {
                    MembershipPackageId = currentPackage.MembershipPackageId,
                    MembershipPackageName = currentPackage.MembershipPackageName,
                    Price = currentPackage.Price,
                    YearlyPrice = currentPackage.YearlyPrice,
                    PercentDiscount = currentPackage.Price > 0
                        ? (int)Math.Round(100 - ((currentPackage.YearlyPrice / (currentPackage.Price * 12)) * 100), 2)
                        : 0,
                    Status = currentPackage.Status,
                    ValidityPeriod = currentPackage.ValidityPeriod,
                    SavingPerMonth = Math.Round(currentPackage.Price > 0
                        ? currentPackage.Price - (currentPackage.YearlyPrice / 12)
                        : 0, 2),
                    Image = currentPackage.Image,
                    Summary = currentPackage.Summary,
                    Permissions = currentPackage.Permissions.Select(p => new PermissionDTO
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        Description = p.Description
                    }).ToList()
                } : null
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

            // 🔍 Kiểm tra giao dịch "pending" gần nhất của user
            // 🔍 Kiểm tra giao dịch "pending" gần nhất của user
            var lastPendingTransaction = await _context.PaymentTransactions
                .Where(pt => pt.UserId == userId && pt.Status == "pending")
                .OrderByDescending(pt => pt.TransactionDate) // Lấy giao dịch gần nhất
                .FirstOrDefaultAsync();

            if (lastPendingTransaction != null)
            {
                if (lastPendingTransaction.MembershipPackageId == request.IdPackage)
                {
                    // Nếu gói trùng với giao dịch pending, trả về link cũ
                    return Ok(new
                    {
                        message = "Bạn đã có một giao dịch đang chờ thanh toán.",
                        pendingUrl = lastPendingTransaction.PaymentLink,
                        transactionId = lastPendingTransaction.PaymentTransactionId
                    });
                }
                else
                {
                    // Nếu chọn gói khác, chỉ báo có giao dịch pending
                    return BadRequest(new
                    {
                        message = "Bạn đã có một giao dịch đang chờ thanh toán.",
                        transactionId = lastPendingTransaction.PaymentTransactionId
                    });
                }
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

                // ✅ Lưu PaymentLink vào database
                paymentTransaction.PaymentLink = approvalUrl;
                _context.PaymentTransactions.Update(paymentTransaction);
                await _context.SaveChangesAsync();

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
