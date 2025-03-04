using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;
using SWD392.Service;

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
        public async Task<ActionResult<IEnumerable<GetMembershipPackageDTO>>> GetMembershipPackages(int pageNumber = 1, int pageSize = 8)
        {
            var totalPackages = await _context.MembershipPackages.CountAsync();

            var packages = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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

            if (!packages.Any())
            {
                return NotFound(new { message = "No membership packages found" });
            }

            var maxPages = (int)Math.Ceiling(totalPackages / (double)pageSize);
            var hasNext = pageNumber < maxPages;

            var pagination = new Pagination(maxPages, hasNext, totalPackages); // Truyền tổng số gói vào

            return Ok(ApiResponse<object>.Success(packages, pagination));
        }

        [HttpGet("PricingPlan")]
        public async Task<ActionResult<IEnumerable<GetMembershipPackageDTO>>> GetActiveMembershipPackages()
        {
            int? userId = null;

            // Kiểm tra nếu có User-Id trong header
            if (Request.Headers.TryGetValue("User-Id", out var userIdHeader) && int.TryParse(userIdHeader, out int parsedUserId))
            {
                userId = parsedUserId;
            }
            else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                // Giải mã token lấy userId từ claims
                var token = authHeader.ToString().Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken != null)
                {
                    var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id");
                    if (idClaim != null && int.TryParse(idClaim.Value, out int tokenUserId))
                    {
                        userId = tokenUserId;
                    }
                }
            }

            // Nếu không có userId -> Mặc định false cho IsActive
            int? userPackage = null;
            if (userId.HasValue)
            {
                userPackage = await _context.UserMemberships
                    .Where(um => um.UserId == userId.Value && um.Status == "active")
                    .Select(um => um.MembershipPackageId)
                    .FirstOrDefaultAsync();
            }

            var packages = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .Where(p => p.Status == "active")
                .Select(p => new GetMembershipPackageDTO
                {
                    MembershipPackageId = p.MembershipPackageId,
                    MembershipPackageName = p.MembershipPackageName,
                    Price = p.Price,
                    Status = p.Status,
                    ValidityPeriod = p.ValidityPeriod,
                    IsActive = (userPackage.HasValue && p.MembershipPackageId == userPackage.Value), 
                    SavingPerMonth = p.Price - p.YearlyPrice/12,
                    Permissions = p.Permissions.Select(perm => new PermissionDTO
                    {
                        PermissionId = perm.PermissionId,
                        PermissionName = perm.PermissionName,
                        Description = perm.Description
                    }).ToList()
                })
                .ToListAsync();

            if (!packages.Any())
            {
                return NotFound(new { message = "No active membership packages found" });
            }

            return Ok(ApiResponse<object>.Success(packages));
        }





        [HttpGet("{id}")]
        public async Task<ActionResult<GetMembershipPackageDTO>> GetMembershipPackageById(int id)
        {
            var package = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(p => p.MembershipPackageId == id);

            if (package == null)
            {
                return NotFound(new { status = "error", message = "Membership package not found" });
            }

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

            return Ok(new { status = "successfully", data = resultDto });
        }


        /// <summary>
        /// Create a new membership package.(Admin only)
        /// </summary>
        /// <remarks>
        /// Accepts package data including package name, price, status, validity period, and a list of permissions.
        /// The permissions should be selected from the list provided by GET /api/permissions.
        /// </remarks>
        /// <param name="dto">The package data.</param>
        /// <response code="201">Membership package created successfully.</response>
        /// <response code="400">Invalid package data.</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult> CreateMembershipPackage([FromBody] CreatePackageDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.MembershipPackageName))
            {
                return BadRequest(new { status = "error", message = "Invalid package data" });
            }

            var package = new MembershipPackage
            {
                MembershipPackageName = dto.MembershipPackageName,
                Price = dto.Price,
                Status = dto.Status,
                ValidityPeriod = dto.ValidityPeriod,
                CreatedTime = DateTime.UtcNow
            };

            if (dto.Permissions != null && dto.Permissions.Any())
            {
                var permissions = await _context.Permissions
                    .Where(p => dto.Permissions.Contains(p.PermissionId))
                    .ToListAsync();
                package.Permissions = permissions;
            }

            _context.MembershipPackages.Add(package);
            await _context.SaveChangesAsync();

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
        /// Update an existing membership package along with its permissions.(Admin only)
        /// </summary>
        /// <remarks>
        /// Updates the membership package identified by the specified ID. You can update the package name, price, status,
        /// validity period and associated permissions. To update permissions, send a list of permission IDs.
        /// </remarks>
        /// <param name="id">The membership package ID.</param>
        /// <param name="dto">The updated package data.</param>
        /// <response code="200">Membership package updated successfully.</response>
        /// <response code="404">Membership package not found.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMembershipPackage(int id, [FromBody] CreatePackageDTO dto)
        {
            
            var package = await _context.MembershipPackages
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(p => p.MembershipPackageId == id);

            if (package == null)
            {
                return NotFound(new { status = "error", message = "Membership package not found" });
            }

            
            if (!string.IsNullOrEmpty(dto.MembershipPackageName))
            {
                package.MembershipPackageName = dto.MembershipPackageName;
            }
          
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

           
            if (dto.Permissions != null)
            {
               
                var newPermissions = await _context.Permissions
                    .Where(p => dto.Permissions.Contains(p.PermissionId))
                    .ToListAsync();

                
                package.Permissions.Clear();

                
                foreach (var permission in newPermissions)
                {
                    package.Permissions.Add(permission);
                }
            }

            await _context.SaveChangesAsync();

           
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

        [Authorize(Roles = "admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdatePackageStatus(int id, [FromBody] EditPackageStatus dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Status))
            {
                return BadRequest(new { status = "error", message = "Invalid status data" });
            }

            var package = await _context.MembershipPackages.FindAsync(id);
            if (package == null)
            {
                return NotFound(new { status = "error", message = "Membership package not found" });
            }

            // Nếu cập nhật sang trạng thái "Active", kiểm tra số lượng gói đã Active
            if (dto.Status == "active")
            {
                int activePackagesCount = await _context.MembershipPackages.CountAsync(p => p.Status == "active");

                if (activePackagesCount >= 3)
                {
                    return BadRequest(new { status = "error", message = "Only 3 membership packages can be active at the same time." });
                }
            }

            package.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { status = "success", message = "Package status updated successfully"});
        }



        /// <summary>
        /// Delete a membership package.(Admin only)
        /// </summary>
        /// <remarks>
        /// Deletes the membership package identified by the specified ID.
        /// </remarks>
        /// <param name="id">The membership package ID.</param>
        /// <response code="200">Membership package deleted successfully.</response>
        /// <response code="404">Membership package not found.</response>
        [Authorize(Roles = "admin")]
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
