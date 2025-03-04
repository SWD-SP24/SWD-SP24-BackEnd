using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal;
using PayPal.Api;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
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

            if (currentMembership != null)
            {
                var currentPackage = await _context.MembershipPackages
                    .FirstOrDefaultAsync(x => x.MembershipPackageId == currentMembership.MembershipPackageId);

                if (currentPackage != null && requestedPackage.Price < currentPackage.Price && requestedPackage.Price == 0)
                {
                    return BadRequest(new { message = "Bạn không thể mua gói thấp hơn gói hiện tại." });
                }
            }
            var startDate = DateTime.UtcNow;
            var validityPeriod = requestedPackage.ValidityPeriod;
            var endDate = startDate.AddDays(validityPeriod);

            var orderDetail = new GetOrderDetailDTO
            {
                MembershipPackageId = idPackage,
                StartDate = startDate,
                EndDate = endDate,
                PaymentTransactionId = null,
                MembershipPackage = new GetMembershipPackageDTO
                {
                     MembershipPackageId = requestedPackage.MembershipPackageId,
                    MembershipPackageName = requestedPackage.MembershipPackageName,
                    Price = requestedPackage.Price,
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

            string previousPackageName = null;

            if (currentMembership != null)
            {
                var currentPackage = await _context.MembershipPackages
                    .FirstOrDefaultAsync(x => x.MembershipPackageId == currentMembership.MembershipPackageId);

                if (currentPackage != null)
                {
                    if (requestedPackage.Price < currentPackage.Price && requestedPackage.Price == 0)
                    {
                        return BadRequest(new { message = "Bạn không thể mua gói có giá thấp hơn gói hiện tại." });
                    }

                    // Nếu nâng cấp lên gói cao hơn, lưu tên gói cũ
                    if (requestedPackage.Price > currentPackage.Price)
                    {
                        previousPackageName = currentPackage.MembershipPackageName;
                    }
                }
            }

            var paymentTransaction = new PaymentTransaction
            {
                UserId = userId,
                MembershipPackageId = idPackage,
                Amount = requestedPackage.Price,
                TransactionDate = DateTime.UtcNow,
                Status = "pending",
                PreviousMembershipPackageName = previousPackageName // Lưu tên gói cũ nếu nâng cấp
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
                    /* return_url = $"https://swd39220250217220816.azurewebsites.net/api/PayPal/execute-payment?idMbPackage={idPackage}",
                     cancel_url = "https://swd39220250217220816.azurewebsites.net/api/PayPal/cancel-payment"*/
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
