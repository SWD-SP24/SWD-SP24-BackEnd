using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayPal.Api;
using PayPal;
using SWD392.Data;
using SWD392.Models;
using SWD392.DTOs.MembershipPackagesDTO;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayPalController : ControllerBase

    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _Context;
        public PayPalController(IConfiguration configuration, AppDbContext Context)
        {
            _configuration = configuration;
            _Context = Context;
        }

        [HttpPost("create-payment")]
        public async Task<string> CreatePayment(MembershipPackage package)
        {
            var mebershipPackage = new GetMembershipPackageDTO
            {
                MembershipPackageId = package.MembershipPackageId,
                MembershipPackageName = package.MembershipPackageName,
                Price = package.Price,
            };
            var total = mebershipPackage.Price;
            var idPackage = mebershipPackage.MembershipPackageId;
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
                    total = total.ToString()
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

            var createdPayment = payment.Create(apiContext);

            var approvalUrl = createdPayment.links.FirstOrDefault(
                link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;

            if (approvalUrl == null)
            {
                throw new Exception("Không tìm thấy URL phê duyệt từ PayPal.");
            }

            return approvalUrl;
        }


        [HttpGet("execute-payment")]
        public IActionResult ExecutePayment(string paymentId, string PayerID, int idMbPackage)
        {
            // Kiểm tra thông tin đầu vào
            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(PayerID))
            {
                return BadRequest(new { message = "Thiếu paymentId hoặc PayerID" });
            }

            var apiContext = PayPalConfiguration.GetAPIContext(_configuration);
            var paymentExecution = new PaymentExecution { payer_id = PayerID };
            var payment = new Payment { id = paymentId };

            try
            {
                // Thực hiện thanh toán PayPal
                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() == "approved")
                {
                    try
                    {
                        // Kiểm tra và lấy token từ Authorization header
                        if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                        {
                            return Unauthorized(new { message = "Không tìm thấy header Authorization" });
                        }

                        var authHeader = HttpContext.Request.Headers["Authorization"][0]; // Lấy giá trị đầu tiên
                        if (string.IsNullOrEmpty(authHeader))
                        {
                            return Unauthorized(new { message = "Token không hợp lệ" });
                        }

                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(authHeader); // Đọc token

                        // Kiểm tra và lấy giá trị "id" từ token
                        var rawId = token.Claims.FirstOrDefault(claim => claim.Type == "id")?.Value;

                        if (string.IsNullOrEmpty(rawId))
                        {
                            return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong token" });
                        }

                        var userId = int.Parse(rawId);

                        // Kiểm tra người dùng tồn tại
                        var user = _Context.Users.FirstOrDefault(x => x.UserId == userId);
                        if (user == null)
                        {
                            return NotFound(new { message = $"Người dùng với userId {userId} không tồn tại" });
                        }

                        // Cập nhật MembershipPackageId
                        user.MembershipPackageId = idMbPackage;
                        _Context.Users.Update(user);
                        _Context.SaveChanges();

                        return Ok(new { message = "Thanh toán thành công", payment = executedPayment });
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        return Unauthorized(new { message = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new { message = "Có lỗi không xác định", error = ex.Message });
                    }
                }

                return BadRequest(new { message = "Thanh toán không thành công" });
            }
            catch (PayPalException ex)
            {
                var errorDetails = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { message = "Có lỗi xảy ra khi thực hiện thanh toán", error = errorDetails });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Có lỗi không xác định", error = ex.Message });
            }
        }









        [HttpGet("cancel-payment")]
        public IActionResult CancelPayment()
        {
            return Ok(new { message = "Thanh toán đã bị hủy" });
        }
    }

}
