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

            // Xác định gói miễn phí (ID = 1)
            int idPackage = 1;
            var requestedPackage = await _context.MembershipPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MembershipPackageId == idPackage);

            if (requestedPackage == null)
            {
                return BadRequest(new { message = "Package not found" });
            }

            if (requestedPackage.Price != 0)
            {
                return BadRequest(new { message = "Gói này không miễn phí." });
            }

            // Kiểm tra membership hiện tại của user
            var currentMembership = await _context.UserMemberships
                .FirstOrDefaultAsync(um => um.UserId == userId && um.EndDate > DateTime.UtcNow);

            if (currentMembership != null && idPackage < currentMembership.MembershipPackageId)
            {
                return BadRequest(new { message = "Bạn không thể mua gói thấp hơn gói hiện tại." });
            }

            // Tạo giao dịch thanh toán với giá trị 0
            var paymentTransaction = new PaymentTransaction
            {
                UserId = userId,
                MembershipPackageId = idPackage,
                Amount = requestedPackage.Price,
                TransactionDate = DateTime.UtcNow,
                Status = "success",
                PaymentId = "FREE"
            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            // Lưu thông tin giá gói
            decimal priceAtPurchase = requestedPackage.Price;
            decimal yearlyPriceAtPurchase = requestedPackage.YearlyPrice;
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = startDate.AddDays(requestedPackage.ValidityPeriod);

            UserMembership newMembership;

            if (currentMembership != null)
            {
                if (currentMembership.MembershipPackageId == idPackage)
                {
                    // Gia hạn gói miễn phí
                    currentMembership.EndDate = currentMembership.EndDate.Value.AddDays(requestedPackage.ValidityPeriod);
                    _context.UserMemberships.Update(currentMembership);
                    newMembership = currentMembership;
                }
                else
                {
                    // Hủy gói cũ, tạo mới gói miễn phí
                    currentMembership.EndDate = DateTime.UtcNow;
                    currentMembership.Status = "expired";
                    _context.UserMemberships.Update(currentMembership);

                    newMembership = new UserMembership
                    {
                        UserId = userId,
                        MembershipPackageId = idPackage,
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = "active",
                        PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                        PriceAtPurchase = priceAtPurchase,
                        YearlyPriceAtPurchase = yearlyPriceAtPurchase
                    };
                    _context.UserMemberships.Add(newMembership);
                }
            }
            else
            {
                // Nếu user chưa có membership → tạo mới
                newMembership = new UserMembership
                {
                    UserId = userId,
                    MembershipPackageId = idPackage,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "active",
                    PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                    PriceAtPurchase = priceAtPurchase,
                    YearlyPriceAtPurchase = yearlyPriceAtPurchase
                };
                _context.UserMemberships.Add(newMembership);
            }

            await _context.SaveChangesAsync();

            // **🚀 Lưu quyền vào UserPermissions**
            // Lấy danh sách quyền của gói membership mới
            var permissions = await _context.Permissions
                .FromSqlRaw(@"SELECT p.* FROM Permissions p 
                  JOIN package_permissions pp ON p.permission_id = pp.permission_id
                  WHERE pp.membership_package_id = {0}", idPackage)
                .ToListAsync();

            if (permissions.Any())
            {
                // Lấy danh sách quyền hiện có của người dùng
                var existingUserPermissions = await _context.UserPermissions
                    .Where(up => up.UserMembershipId == newMembership.UserMembershipId)
                    .Select(up => up.PermissionId)
                    .ToListAsync();

                // Lọc ra các quyền chưa có trong UserPermissions
                var newPermissions = permissions
                    .Where(p => !existingUserPermissions.Contains(p.PermissionId))
                    .Select(p => new UserPermission
                    {
                        UserMembershipId = newMembership.UserMembershipId,
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        PermissionDescription = p.Description
                    })
                    .ToList();

                // Chỉ thêm quyền nếu có quyền mới
                if (newPermissions.Any())
                {
                    _context.UserPermissions.AddRange(newPermissions);
                    await _context.SaveChangesAsync();
                }
            }


            // Cập nhật MembershipPackageId cho user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                user.MembershipPackageId = idPackage;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Mua gói miễn phí thành công", transactionId = paymentTransaction.PaymentTransactionId });
        }
    }
}
