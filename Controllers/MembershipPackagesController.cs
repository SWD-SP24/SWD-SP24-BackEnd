using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipPackagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MembershipPackagesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all membership packages.
        /// </summary>
        /// <remarks>
        /// Returns a list of membership packages with their associated permissions.
        /// </remarks>
        /// <response code="200">Returns the list of membership packages.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetMembershipPackageDTO>>> GetMembershipPackages()
        {
            var packages = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .Select(p => new GetMembershipPackageDTO
                {
                    MembershipPackageId = p.MembershipPackageId,
                    MembershipPackageName = p.MembershipPackageName,
                    Price = p.Price,
                    Status = p.Status,
                    ValidityPeriod = p.ValidityPeriod,
                    Permissions = p.Permissions.Select(perm => new PermissionDTO
                    {
                        PermissionId = perm.PermissionId,
                        PermissionName = perm.PermissionName,
                        Description = perm.Description
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { status = "success", data = packages });
        }

        /// <summary>
        /// Create a new membership package.
        /// </summary>
        /// <remarks>
        /// Accepts package data including package name, price, status, validity period, and a list of permissions.
        /// The permissions should be selected from the list provided by GET /api/permissions.
        /// </remarks>
        /// <param name="dto">The package data.</param>
        /// <response code="201">Membership package created successfully.</response>
        /// <response code="400">Invalid package data.</response>
        [HttpPost]
        public async Task<ActionResult> CreateMembershipPackage([FromBody] CreatePackageDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.MembershipPackageName))
            {
                return BadRequest(new { status = "error", message = "Invalid package data" });
            }

            // Tạo mới đối tượng MembershipPackage từ DTO
            var package = new MembershipPackage
            {
                MembershipPackageName = dto.MembershipPackageName,
                Price = dto.Price,
                Status = dto.Status,
                ValidityPeriod = dto.ValidityPeriod,
                CreatedTime = DateTime.UtcNow
            };

            // Nếu có danh sách quyền, lấy các quyền tương ứng theo PermissionIds
            if (dto.Permissions != null && dto.Permissions.Any())
            {
                var permissions = await _context.Permissions
                    .Where(p => dto.Permissions.Contains(p.PermissionId))
                    .ToListAsync();
                package.Permissions = permissions;
            }

            _context.MembershipPackages.Add(package);
            await _context.SaveChangesAsync();

            // Sau khi lưu, MembershipPackageId đã được tự động tạo bởi DB
            var resultDto = new GetMembershipPackageDTO
            {
                MembershipPackageId = package.MembershipPackageId,
                MembershipPackageName = package.MembershipPackageName,
                Price = package.Price,
                Status = package.Status,
                ValidityPeriod = package.ValidityPeriod,
                Permissions = package.Permissions.Select(p => new PermissionDTO
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description
                }).ToList()
            };

            return CreatedAtAction(nameof(GetMembershipPackages), new { id = package.MembershipPackageId },
                new { status = "success", data = resultDto });
        }

        /// <summary>
        /// Update an existing membership package along with its permissions.
        /// </summary>
        /// <remarks>
        /// Updates the membership package identified by the specified ID. You can update the package name, price, status,
        /// validity period and associated permissions. To update permissions, send a list of permission IDs.
        /// </remarks>
        /// <param name="id">The membership package ID.</param>
        /// <param name="dto">The updated package data.</param>
        /// <response code="200">Membership package updated successfully.</response>
        /// <response code="404">Membership package not found.</response>
        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateMembershipPackage(int id, [FromBody] CreatePackageDTO dto)
        {
            // Tìm package theo ID và include các quyền hiện có
            var package = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(p => p.MembershipPackageId == id);

            if (package == null)
            {
                return NotFound(new { status = "error", message = "Membership package not found" });
            }

            // Cập nhật các trường nếu có dữ liệu mới được gửi lên
            if (!string.IsNullOrEmpty(dto.MembershipPackageName))
            {
                package.MembershipPackageName = dto.MembershipPackageName;
            }
            // Lưu ý: Nếu giá trị Price là 0 và 0 là giá hợp lệ (ví dụ: gói miễn phí), hãy điều chỉnh logic này theo nhu cầu của bạn
            if (dto.Price != 0)
            {
                package.Price = dto.Price;
            }
            if (!string.IsNullOrEmpty(dto.Status))
            {
                package.Status = dto.Status;
            }
            if (dto.ValidityPeriod != 0)
            {
                package.ValidityPeriod = dto.ValidityPeriod;
            }

            // Cập nhật quyền nếu danh sách PermissionIds được gửi
            if (dto.Permissions != null)
            {
                // Lấy danh sách quyền từ DB dựa trên PermissionIds
                var newPermissions = await _context.Permissions
                    .Where(p => dto.Permissions.Contains(p.PermissionId))
                    .ToListAsync();

                // Xóa các liên kết hiện có trong bảng trung gian
                package.Permissions.Clear();

                // Thêm các quyền mới
                foreach (var permission in newPermissions)
                {
                    package.Permissions.Add(permission);
                }
            }

            await _context.SaveChangesAsync();

            // Chuẩn bị dữ liệu trả về theo DTO GetMembershipPackageDTO
            var resultDto = new GetMembershipPackageDTO
            {
                MembershipPackageId = package.MembershipPackageId,
                MembershipPackageName = package.MembershipPackageName,
                Price = package.Price,
                Status = package.Status,
                ValidityPeriod = package.ValidityPeriod,
                Permissions = package.Permissions.Select(p => new PermissionDTO
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description
                }).ToList()
            };

            return Ok(new { status = "success", data = resultDto });
        }

        /// <summary>
        /// Delete a membership package.
        /// </summary>
        /// <remarks>
        /// Deletes the membership package identified by the specified ID.
        /// </remarks>
        /// <param name="id">The membership package ID.</param>
        /// <response code="200">Membership package deleted successfully.</response>
        /// <response code="404">Membership package not found.</response>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMembershipPackage(int id)
        {
            // Load gói thành viên cùng với danh sách quyền
            var package = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(p => p.MembershipPackageId == id);

            if (package == null)
            {
                return NotFound(new { status = "error", message = "Membership package not found" });
            }

            
            package.Permissions.Clear();
            await _context.SaveChangesAsync();

            
            _context.MembershipPackages.Remove(package);
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Membership package deleted successfully" });
        }

    }
}
