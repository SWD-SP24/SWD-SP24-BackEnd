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
using Azure.Core;
using Microsoft.IdentityModel.Tokens;

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
                YearlyPrice = package.YearlyPrice,
            };
            var total = mebershipPackage.YearlyPrice;
            var idPackage = mebershipPackage.MembershipPackageId;

            // Giả sử bạn có thể lấy userMembership từ đâu đó
            var userMembership = _Context.UserMemberships
                .FirstOrDefault(um => um.MembershipPackageId == idPackage); // Lấy UserMembership theo idPackage, bạn có thể thay đổi logic này theo yêu cầu

            if (userMembership == null)
            {
                throw new Exception("Không tìm thấy thông tin thành viên.");
            }

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
                    return_url = $"https://growplus.hungngblog.com/api/PayPal/execute-payment?idMbPackage={Uri.EscapeDataString(idPackage.ToString())}&paymentType={Uri.EscapeDataString("paypal")}&userMembershipId={Uri.EscapeDataString(userMembership.UserMembershipId.ToString())}",
                    cancel_url = "https://growplus.hungngblog.com//api/PayPal/cancel-payment"
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
        public IActionResult ExecutePayment(string paymentId, string PayerID, int idMbPackage, int validityDays)
        {
            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(PayerID))
            {
                return BadRequest(new { message = "Thiếu paymentId hoặc PayerID" });
            }

            var apiContext = PayPalConfiguration.GetAPIContext(_configuration);
            var paymentExecution = new PaymentExecution { payer_id = PayerID };
            var payment = new Payment { id = paymentId };

            try
            {
                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() == "approved")
                {
                    try
                    {
                        var paymentTransaction = _Context.PaymentTransactions
                            .FirstOrDefault(pt => pt.PaymentId == paymentId);

                        if (paymentTransaction == null)
                        {
                            return NotFound(new { message = $"Không tìm thấy giao dịch với PaymentId {paymentId}" });
                        }

                        if (paymentTransaction.Status == "success")
                        {
                            return BadRequest(new { message = "Giao dịch này đã được xử lý trước đó." });
                        }

                        var userId = paymentTransaction.UserId;
                        var user = _Context.Users.FirstOrDefault(x => x.UserId == userId);

                        if (user == null)
                        {
                            return NotFound(new { message = $"Người dùng với userId {userId} không tồn tại" });
                        }

                        paymentTransaction.Status = "success";
                        _Context.PaymentTransactions.Update(paymentTransaction);

                        var membershipPackage = _Context.MembershipPackages
                            .FirstOrDefault(mp => mp.MembershipPackageId == idMbPackage);

                        if (membershipPackage == null)
                        {
                            return NotFound(new { message = $"Không tìm thấy gói thành viên với ID {idMbPackage}" });
                        }

                        var activeMembership = _Context.UserMemberships
                            .Where(um => um.UserId == userId && um.Status == "active" && um.EndDate > DateTime.UtcNow)
                            .FirstOrDefault();

                        UserMembership newMembership;

                        if (activeMembership != null)
                        {
                            if (activeMembership.MembershipPackageId == idMbPackage)
                            {
                                activeMembership.EndDate = activeMembership.EndDate.Value.AddDays(membershipPackage.ValidityPeriod);
                                _Context.UserMemberships.Update(activeMembership);
                                newMembership = activeMembership; // Dùng membership cũ
                            }
                            else
                            {
                                activeMembership.EndDate = DateTime.UtcNow;
                                activeMembership.Status = "expired";
                                _Context.UserMemberships.Update(activeMembership);

                                newMembership = new UserMembership
                                {
                                    UserId = userId,
                                    MembershipPackageId = idMbPackage,
                                    StartDate = DateTime.UtcNow,
                                    EndDate = DateTime.UtcNow.AddDays(validityDays),
                                    Status = "active",
                                    PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                                     PriceAtPurchase = membershipPackage.Price,
                                    YearlyPriceAtPurchase = membershipPackage.YearlyPrice
                                };
                                _Context.UserMemberships.Add(newMembership);
                            }
                        }
                        else
                        {
                            newMembership = new UserMembership
                            {
                                UserId = userId,
                                MembershipPackageId = idMbPackage,
                                StartDate = DateTime.UtcNow,
                                EndDate = DateTime.UtcNow.AddDays(membershipPackage.ValidityPeriod),
                                Status = "active",
                                PaymentTransactionId = paymentTransaction.PaymentTransactionId
                            };
                            _Context.UserMemberships.Add(newMembership);
                        }

                        _Context.SaveChanges(); // Lưu membership để có ID mới

                        // 🔥 **Lấy danh sách quyền thông qua bảng trung gian**
                        var permissions = _Context.Permissions
    .FromSqlRaw(@"SELECT p.* FROM Permissions p 
                  JOIN package_permissions pp ON p.permission_id = pp.permission_id
                  WHERE pp.membership_package_id = {0}", idMbPackage)
    .ToList();


                        // Thêm quyền vào `UserPermissions`
                        var userPermissions = permissions.Select(p => new UserPermission
                        {
                            UserMembershipId = newMembership.UserMembershipId,
                            PermissionId = p.PermissionId,
                            PermissionName = p.PermissionName,
                            PermissionDescription = p.Description
                        }).ToList();

                        _Context.UserPermissions.AddRange(userPermissions);

                        // ✅ Cập nhật MembershipPackageId trong Users
                        user.MembershipPackageId = idMbPackage;
                        _Context.Users.Update(user);

                        _Context.SaveChanges(); // Lưu toàn bộ dữ liệu

                        return Redirect($"https://growplus.hungngblog.com/upgrade-plan/confirm?paymentId={paymentId}");
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
