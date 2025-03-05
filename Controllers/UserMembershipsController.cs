﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.DTOs.UserMembershipDTO;
using SWD392.Models;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserMembershipsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserMembershipsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UserMemberships
        [Authorize(Roles = "member")]
        [HttpGet("CurrentPackage")]
        public async Task<ActionResult<GetCurrentPackageDTO>> GetCurrentMembership()
        {
            var userIdString = User.FindFirstValue("id");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var userMembership = await _context.UserMemberships
                .Where(um => um.UserId == userId && um.Status == "Active")
                .Include(um => um.MembershipPackage)
                .ThenInclude(mp => mp.Permissions)
                .OrderByDescending(um => um.StartDate) // Lấy gói mới nhất
                .FirstOrDefaultAsync();

            if (userMembership == null)
            {
                return NotFound("No active membership found.");
            }

            return Ok(new GetCurrentPackageDTO
            {
                StartDate = userMembership.StartDate,
                EndDate = userMembership.EndDate,
                Status = userMembership.Status,
                MembershipPackage = new GetMembershipPackageDTO
                {
                    MembershipPackageId = userMembership.MembershipPackage.MembershipPackageId,
                    MembershipPackageName = userMembership.MembershipPackage.MembershipPackageName,
                    Price = userMembership.MembershipPackage.Price,
                    ValidityPeriod = userMembership.MembershipPackage.ValidityPeriod,
                    YearlyPrice = userMembership.MembershipPackage.YearlyPrice,
                    Image = userMembership.MembershipPackage.Image,
                    Permissions = userMembership.MembershipPackage.Permissions.Select(p => new PermissionDTO
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        Description = p.Description
                    }).ToList()
                }
            });
        }


    }
}
