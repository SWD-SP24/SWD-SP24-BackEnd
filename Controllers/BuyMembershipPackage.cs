﻿using Microsoft.AspNetCore.Authorization;
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



        [HttpPost]
        public async Task<IActionResult> Post([FromBody] int idPackage)
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new { message = "Authorization header missing" });
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(authHeader);
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
                if (idPackage < currentMembership.MembershipPackageId)
                {
                    return BadRequest(new { message = "Bạn không thể mua gói thấp hơn gói hiện tại." });
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
