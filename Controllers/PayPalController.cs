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
                    return_url = "https://swd39220250217220816.azurewebsites.net/api/PayPal/execute-payment?idMbPackage=" + idPackage,
                    cancel_url = "https://swd39220250217220816.azurewebsites.net/api/PayPal/cancel-payment"
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
                // Thực hiện thanh toán qua PayPal
                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() == "approved")
                {
                    try
                    {
                        // Lấy PaymentTransaction dựa trên PaymentId
                        var paymentTransaction = _Context.PaymentTransactions
                            .FirstOrDefault(pt => pt.PaymentId == paymentId);

                        if (paymentTransaction == null)
                        {
                            return NotFound(new { message = $"Không tìm thấy giao dịch với PaymentId {paymentId}" });
                        }

                        // Nếu giao dịch đã được xử lý trước đó thì không xử lý lại
                        if (paymentTransaction.Status == "success")
                        {
                            return BadRequest(new { message = "Giao dịch này đã được xử lý trước đó." });
                        }

                        var userId = paymentTransaction.UserId;

                        // Kiểm tra người dùng tồn tại
                        var user = _Context.Users.FirstOrDefault(x => x.UserId == userId);
                        if (user == null)
                        {
                            return NotFound(new { message = $"Người dùng với userId {userId} không tồn tại" });
                        }

                        // Cập nhật trạng thái PaymentTransaction thành "success"
                        paymentTransaction.Status = "success";
                        _Context.PaymentTransactions.Update(paymentTransaction);
                        _Context.SaveChanges();

                        // Lấy thông tin chi tiết của gói thành viên mới
                        var membershipPackage = _Context.MembershipPackages
                            .FirstOrDefault(mp => mp.MembershipPackageId == idMbPackage);

                        if (membershipPackage == null)
                        {
                            return NotFound(new { message = $"Không tìm thấy gói thành viên với ID {idMbPackage}" });
                        }

                        // Lấy UserMembership đang active của người dùng (nếu có)
                        var activeMembership = _Context.UserMemberships
                            .Where(um => um.UserId == userId && um.Status == "active" && um.EndDate > DateTime.UtcNow)
                            .FirstOrDefault();

                        if (activeMembership != null)
                        {
                            if (activeMembership.MembershipPackageId == idMbPackage)
                            {
                                // Trường hợp gia hạn gói: Nếu gói hiện tại giống với gói gửi lên API,
                                // thì gia hạn EndDate bằng cách cộng thêm ValidityPeriod.
                                activeMembership.EndDate = activeMembership.EndDate.Value.AddDays(membershipPackage.ValidityPeriod);
                                _Context.UserMemberships.Update(activeMembership);
                                _Context.SaveChanges();
                            }

                            else
                            {
                                // Trường hợp nâng cấp gói:
                                // Kết thúc gói hiện tại ngay lập tức.
                                activeMembership.EndDate = DateTime.UtcNow;
                                activeMembership.Status = "expired";
                                _Context.UserMemberships.Update(activeMembership);
                                _Context.SaveChanges();

                                // Tạo mới UserMembership cho gói nâng cấp
                                var newMembership = new UserMembership
                                {
                                    UserId = userId,
                                    MembershipPackageId = idMbPackage,
                                    StartDate = DateTime.UtcNow,
                                    EndDate = DateTime.UtcNow.AddDays(membershipPackage.ValidityPeriod),
                                    Status = "active",
                                    PaymentTransactionId = paymentTransaction.PaymentTransactionId
                                };
                                _Context.UserMemberships.Add(newMembership);
                                _Context.SaveChanges();
                            }
                        }
                        else
                        {
                            // Nếu người dùng chưa có UserMembership active, tạo mới bản ghi
                            var newMembership = new UserMembership
                            {
                                UserId = userId,
                                MembershipPackageId = idMbPackage,
                                StartDate = DateTime.UtcNow,
                                EndDate = DateTime.UtcNow.AddDays(membershipPackage.ValidityPeriod),
                                Status = "active",
                                PaymentTransactionId = paymentTransaction.PaymentTransactionId
                            };
                            _Context.UserMemberships.Add(newMembership);
                            _Context.SaveChanges();
                        }

                        // Cập nhật MembershipPackageId của người dùng trong bảng Users
                        user.MembershipPackageId = idMbPackage;
                        _Context.Users.Update(user);
                        _Context.SaveChanges();

                        return Ok(new { message = "Thanh toán thành công", payment = executedPayment });
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new { message = "Có lỗi không xác định khi xử lý giao dịch", error = ex.Message });
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
