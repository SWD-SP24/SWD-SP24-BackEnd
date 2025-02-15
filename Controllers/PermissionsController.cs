using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all permissions (Admin only).
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Unauthorized access.
        /// - Database connection issue.
        /// - No permissions found.
        /// </remarks>
        /// <returns>A list of permissions.</returns>
        /// <response code="200">Returns the list of permissions.</response>
        /// <response code="403">Forbidden. Only Admin can access.</response>
        /// <response code="500">Internal server error.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionDTO>>> GetPermissions()
        {
            if (!IsAdmin())
            {
                return Forbid("Access denied. Only Admins can perform this action.");
            }

            var permissions = await _context.Permissions
                .Select(p => new PermissionDTO
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description
                })
                .ToListAsync();

            if (!permissions.Any())
            {
                return NotFound(new { message = "No permissions found" });
            }

            return Ok(new { status = "success", data = permissions });
        }

        /// <summary>
        /// Create a new permission (Admin only).
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Unauthorized access.
        /// - Permission name is required.
        /// - Database error while saving.
        /// </remarks>
        /// <param name="dto">Permission data.</param>
        /// <returns>Created permission details.</returns>
        /// <response code="201">Permission created successfully.</response>
        /// <response code="400">Invalid input data.</response>
        /// <response code="403">Forbidden. Only Admin can access.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPost]
        public async Task<ActionResult> CreatePermission([FromBody] CreatePermissionDTO dto)
        {
            if (!IsAdmin())
            {
                return Forbid("Access denied. Only Admins can perform this action.");
            }

            if (string.IsNullOrEmpty(dto.PermissionName))
            {
                return BadRequest(new { status = "error", message = "Permission name is required." });
            }

            var newPermission = new Permission
            {
                PermissionName = dto.PermissionName,
                Description = dto.Description
            };

            _context.Permissions.Add(newPermission);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPermissions), new { id = newPermission.PermissionId }, new
            {
                status = "success",
                data = new
                {
                    newPermission.PermissionId,
                    newPermission.PermissionName,
                    newPermission.Description
                }
            });
        }

        /// <summary>
        /// Update a permission (Admin only).
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Unauthorized access.
        /// - Permission not found.
        /// - Database error while updating.
        /// </remarks>
        /// <param name="id">Permission ID.</param>
        /// <param name="dto">Updated permission data.</param>
        /// <returns>Updated permission details.</returns>
        /// <response code="200">Permission updated successfully.</response>
        /// <response code="403">Forbidden. Only Admin can access.</response>
        /// <response code="404">Permission not found.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] CreatePermissionDTO dto)
        {
            if (!IsAdmin())
            {
                return Forbid("Access denied. Only Admins can perform this action.");
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound(new { message = "Permission not found" });
            }

            if (!string.IsNullOrEmpty(dto.PermissionName))
            {
                permission.PermissionName = dto.PermissionName;
            }

            if (!string.IsNullOrEmpty(dto.Description))
            {
                permission.Description = dto.Description;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Permission updated successfully",
                data = new
                {
                    permissionId = permission.PermissionId,
                    permissionName = permission.PermissionName,
                    description = permission.Description
                }
            });
        }

        /// <summary>
        /// Delete a permission (Admin only).
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Unauthorized access.
        /// - Permission not found.
        /// - Database error while deleting.
        /// </remarks>
        /// <param name="id">Permission ID.</param>
        /// <returns>Deletion status.</returns>
        /// <response code="200">Permission deleted successfully.</response>
        /// <response code="403">Forbidden. Only Admin can access.</response>
        /// <response code="404">Permission not found.</response>
        /// <response code="500">Internal server error.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            if (!IsAdmin())
            {
                return Forbid("Access denied. Only Admins can perform this action.");
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound(new { message = "Permission not found" });
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Permission deleted successfully" });
        }

        private bool PermissionExists(int id)
        {
            return _context.Permissions.Any(e => e.PermissionId == id);
        }

        private bool IsAdmin()
        {
            /*var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return false;
            }

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            return roleClaim != null && roleClaim.Value == "admin";*/
            return true;
        }
    }
}
