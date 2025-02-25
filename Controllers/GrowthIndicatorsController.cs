using System;
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

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<GrowthIndicatorDTO>>>> GetGrowthIndicators([FromQuery] int childrenId)
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

            var growthIndicators = await _context.GrowthIndicators
                                                 .Where(gi => gi.ChildrenId == childrenId)
                                                 .ToListAsync();

            var growthIndicatorDtos = growthIndicators.Select(gi => gi.ToGrowthIndicatorDto());

            return Ok(ApiResponse<IEnumerable<GrowthIndicatorDTO>>.Success(growthIndicatorDtos));
        }


        // GET: api/GrowthIndicators/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GrowthIndicator>> GetGrowthIndicator(int id)
        {
            var growthIndicator = await _context.GrowthIndicators.FindAsync(id);

            if (growthIndicator == null)
            {
                return NotFound();
            }

            return growthIndicator;
        }

        // PUT: api/GrowthIndicators/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrowthIndicator(int id, GrowthIndicator growthIndicator)
        {
            if (id != growthIndicator.GrowthIndicatorsId)
            {
                return BadRequest();
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
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/GrowthIndicators
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GrowthIndicator>> PostGrowthIndicator(GrowthIndicator growthIndicator)
        {
            _context.GrowthIndicators.Add(growthIndicator);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGrowthIndicator", new { id = growthIndicator.GrowthIndicatorsId }, growthIndicator);
        }

        // DELETE: api/GrowthIndicators/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrowthIndicator(int id)
        {
            var growthIndicator = await _context.GrowthIndicators.FindAsync(id);
            if (growthIndicator == null)
            {
                return NotFound();
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
