using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.Models;

namespace SWD392.Controllers
{
    public class AutoFreeMembershipPackage : ControllerBase
    {
        private readonly AppDbContext _context;
        public AutoFreeMembershipPackage(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("auto-purchase-free-package")]
        public async Task<IActionResult> AutoPurchaseFreePackage(string id)
        {
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
    }
}
