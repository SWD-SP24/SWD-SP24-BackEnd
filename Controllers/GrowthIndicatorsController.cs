﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.GrowthIndicatorDTO;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrowthIndicatorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GrowthIndicatorsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of growth indicators for a specified child, optionally filtered by a time range. (Authorized only)
        /// </summary>
        /// <param name="childrenId">The ID of the child whose growth indicators are to be retrieved.</param>
        /// <param name="startTime">The optional start time to filter the growth indicators.</param>
        /// <param name="endTime">The optional end time to filter the growth indicators.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing a list of <see cref="GrowthIndicatorDTO"/> objects.</returns>
        /// <response code="200">Returns the list of growth indicators.</response>
        /// <response code="401">If the user is not authorized.</response>
        /// <response code="404">If the child is not found.</response>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<GrowthIndicatorDTO>>>> GetGrowthIndicators(
            [FromQuery] int childrenId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            var child = await _context.Children.FindAsync(childrenId);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to view this child's growth indicators"));
            }

            var query = _context.GrowthIndicators.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(gi => gi.RecordTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(gi => gi.RecordTime <= endTime.Value);
            }

            var growthIndicators = await query.Where(gi => gi.ChildrenId == childrenId).ToListAsync();

            var growthIndicatorDtos = growthIndicators.Select(gi => gi.ToGrowthIndicatorDto());

            return Ok(ApiResponse<IEnumerable<GrowthIndicatorDTO>>.Success(growthIndicatorDtos));
        }

        /// <summary>
        /// Retrieves a specific growth indicator by ID (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Growth indicator not found
        /// - Unauthorized to view this growth indicator
        /// </remarks>
        /// <param name="id">The ID of the growth indicator to retrieve.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the <see cref="GrowthIndicatorDTO"/> object.</returns>
        /// <response code="200">Returns the growth indicator.</response>
        /// <response code="401">Unauthorized if the user is not authorized to view the growth indicator.</response>
        /// <response code="404">Not found if the growth indicator does not exist.</response>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<GrowthIndicatorDTO>>> GetGrowthIndicator(int id)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            var growthIndicator = await _context.GrowthIndicators
                .Include(gi => gi.Children)
                .FirstOrDefaultAsync(gi => gi.GrowthIndicatorsId == id);

            if (growthIndicator == null)
            {
                return NotFound(ApiResponse<object>.Error("Growth indicator not found"));
            }

            if (growthIndicator.Children.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to view this growth indicator"));
            }

            var growthIndicatorDto = growthIndicator.ToGrowthIndicatorDto();
            return Ok(ApiResponse<GrowthIndicatorDTO>.Success(growthIndicatorDto));
        }

        /// <summary>
        /// Updates a growth indicator (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Growth indicator not found
        /// - Unauthorized to edit this growth indicator
        /// </remarks>
        /// <param name="id">The ID of the growth indicator to update.</param>
        /// <param name="growthIndicatorDto">The DTO containing the updated growth indicator data.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        /// <response code="200">Growth indicator updated successfully.</response>
        /// <response code="400">Bad request if the ID does not match the growth indicator ID.</response>
        /// <response code="401">Unauthorized if the user is not authorized to edit the growth indicator.</response>
        /// <response code="404">Not found if the growth indicator does not exist.</response>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrowthIndicator(int id, EditGrowthIndicatorDTO growthIndicatorDto)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            var growthIndicator = await _context.GrowthIndicators
                .Include(gi => gi.Children)
                .FirstOrDefaultAsync(gi => gi.GrowthIndicatorsId == id);

            if (growthIndicator == null)
            {
                return NotFound(ApiResponse<object>.Error("Growth indicator not found"));
            }

            if (growthIndicator.Children.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to edit this growth indicator"));
            }

            // Update growth indicator properties with non-null values from growthIndicatorDto
            if (growthIndicatorDto.Height.HasValue)
            {
                growthIndicator.Height = growthIndicatorDto.Height.Value;
            }
            if (growthIndicatorDto.Weight.HasValue)
            {
                growthIndicator.Weight = growthIndicatorDto.Weight.Value;
            }
            if (growthIndicatorDto.RecordTime.HasValue)
            {
                growthIndicator.RecordTime = growthIndicatorDto.RecordTime.Value;
            }

            // Calculate BMI if either height or weight is provided
            if (growthIndicatorDto.Height.HasValue || growthIndicatorDto.Weight.HasValue)
            {
                decimal height = growthIndicatorDto.Height ?? growthIndicator.Height;
                decimal weight = growthIndicatorDto.Weight ?? growthIndicator.Weight;

                decimal heightInMeters = height / 100.0M;
                decimal bmi = weight / (heightInMeters * heightInMeters);
                growthIndicator.Bmi = (int)Math.Round(bmi);
            }

            // Calculate BMI if both height and weight are provided
            if (growthIndicatorDto.Height.HasValue && growthIndicatorDto.Weight.HasValue)
            {
                decimal heightInMeters = growthIndicatorDto.Height.Value / 100.0M;
                decimal bmi = growthIndicatorDto.Weight.Value / (heightInMeters * heightInMeters);
                growthIndicator.Bmi = (int)Math.Round(bmi);
            }

            _context.Entry(growthIndicator).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GrowthIndicatorExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Growth indicator not found"));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to edit growth indicator"));
            }

            var growthIndicatorDTO = growthIndicator.ToGrowthIndicatorDto();
            return Ok(ApiResponse<GrowthIndicatorDTO>.Success(growthIndicatorDTO));
        }


        /// <summary>
        /// Creates a new growth indicator (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Child not found
        /// - Unauthorized to add growth indicator for this child
        /// </remarks>
        /// <param name="growthIndicatorDto">The DTO containing the new growth indicator data.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created <see cref="GrowthIndicatorDTO"/> object.</returns>
        /// <response code="201">Growth indicator created successfully.</response>
        /// <response code="401">Unauthorized if the user is not authorized to add the growth indicator.</response>
        /// <response code="404">Not found if the child does not exist.</response>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<GrowthIndicatorDTO>>> PostGrowthIndicator(CreateGrowthIndicatorDTO growthIndicatorDto)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            var child = await _context.Children.FindAsync(growthIndicatorDto.ChildrenId);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to add growth indicator for this child"));
            }

            // Calculate BMI
            decimal heightInMeters = growthIndicatorDto.Height / 100.0M;
            decimal bmi = growthIndicatorDto.Weight / (heightInMeters * heightInMeters);

            var growthIndicator = new GrowthIndicator
            {
                Height = growthIndicatorDto.Height,
                Weight = growthIndicatorDto.Weight,
                Bmi = (int)Math.Round(bmi), // TODO: change BMI type to not integer
                RecordTime = growthIndicatorDto.RecordTime,
                ChildrenId = growthIndicatorDto.ChildrenId
            };

            _context.GrowthIndicators.Add(growthIndicator);
            await _context.SaveChangesAsync();

            var growthIndicatorDTO = growthIndicator.ToGrowthIndicatorDto();
            return CreatedAtAction("GetGrowthIndicator", new { id = growthIndicator.GrowthIndicatorsId }, ApiResponse<GrowthIndicatorDTO>.Success(growthIndicatorDTO));
        }


        /// <summary>
        /// Deletes a specific growth indicator by ID (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Growth indicator not found
        /// - Unauthorized to delete this growth indicator
        /// </remarks>
        /// <param name="id">The ID of the growth indicator to delete.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> indicating the result of the operation.</returns>
        /// <response code="204">Growth indicator deleted successfully.</response>
        /// <response code="401">Unauthorized if the user is not authorized to delete the growth indicator.</response>
        /// <response code="404">Not found if the growth indicator does not exist.</response>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteGrowthIndicator(int id)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            var growthIndicator = await _context.GrowthIndicators
                .Include(gi => gi.Children)
                .FirstOrDefaultAsync(gi => gi.GrowthIndicatorsId == id);

            if (growthIndicator == null)
            {
                return NotFound(ApiResponse<object>.Error("Growth indicator not found"));
            }

            if (growthIndicator.Children.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to delete this growth indicator"));
            }

            _context.GrowthIndicators.Remove(growthIndicator);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GrowthIndicatorExists(int id)
        {
            return _context.GrowthIndicators.Any(e => e.GrowthIndicatorsId == id);
        }

        private async Task<User> ValidateJwtToken(string authHeader)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring("Bearer ".Length).Trim() : authHeader;

            var jwtToken = handler.ReadJwtToken(token);

            // Check if token has expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
                throw new UnauthorizedAccessException("JWT token has expired");

            var rawId = jwtToken.Claims.First(claim => claim.Type == "id").Value;
            var id = int.Parse(rawId);

            var user = await _context.Users.FindAsync(id) ?? throw new UnauthorizedAccessException("Invalid JWT key");
            return user;
        }
    }
}
