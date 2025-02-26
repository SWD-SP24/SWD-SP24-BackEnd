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
using SWD392.DTOs.ChildDTO;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChildrenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChildrenController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Children
        /// <summary>
        /// Get all children of the currently logged-in user (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">Children retrieved</response>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<Child>>>> GetChildren([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 8)
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

            var query = _context.Children.Where(c => c.MemberId == user.UserId);

            var totalItems = await query.CountAsync();
            var children = await query.Skip((pageNumber - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            var lastVisiblePage = (int)Math.Ceiling(totalItems / (double)pageSize);
            var hasNextPage = pageNumber < lastVisiblePage;

            var pagination = new Pagination(lastVisiblePage, hasNextPage);

            var childrenDTOs = children.Select(child => child.ToGetChildDTO());

            return Ok(ApiResponse<IEnumerable<GetChildDTO>>.Success(childrenDTOs, pagination));
        }

        // GET: api/Children/admin
        /// <summary>
        /// Get all children (Admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// </remarks>
        /// <response code="200">Children retrieved</response>
        [Authorize(Roles = "admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<GetChildDTO>>>> GetAllChildrenAdmin([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 8)
        {
            var query = _context.Children.AsQueryable();

            var totalItems = await query.CountAsync();
            var children = await query.Skip((pageNumber - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            var lastVisiblePage = (int)Math.Ceiling(totalItems / (double)pageSize);
            var hasNextPage = pageNumber < lastVisiblePage;

            var pagination = new Pagination(lastVisiblePage, hasNextPage);

            var childrenDTOs = children.Select(child => child.ToGetChildDTO());

            return Ok(ApiResponse<IEnumerable<GetChildDTO>>.Success(childrenDTOs, pagination));
        }

        // GET: api/Children/5
        /// <summary>
        /// Get a child by ID (Admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Child not found
        /// </remarks>
        /// <response code="200">Child retrieved</response>
        /// <response code="404">Child not found</response>
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<GetChildDTO>>> GetChildAdmin(int id)
        {
            var child = await _context.Children.FindAsync(id);

            if (child == null)
            {
                return NotFound(ApiResponse<GetChildDTO>.Error("Child not found"));
            }

            var childDTO = child.ToGetChildDTO();
            return Ok(ApiResponse<GetChildDTO>.Success(childDTO));
        }


        // PUT: api/Children/edit/{id}
        /// <summary>
        /// Edit a child (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Child not found
        /// - Unauthorized to edit this child
        /// </remarks>
        /// <response code="200">Child edited</response>
        [Authorize]
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> EditChild(int id, EditChildDTO childDto)
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

            var child = await _context.Children.FindAsync(id);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to edit this child"));
            }

            // Update child properties with non-null values from childDto
            if (childDto.FullName != null)
            {
                child.FullName = childDto.FullName;
            }
            if (childDto.Avatar != null)
            {
                child.Avatar = childDto.Avatar;
            }
            if (childDto.Dob.HasValue)
            {
                child.Dob = childDto.Dob.Value;
            }
            if (childDto.BloodType != null)
            {
                child.BloodType = childDto.BloodType;
            }
            if (childDto.Allergies != null)
            {
                child.Allergies = childDto.Allergies;
            }
            if (childDto.ChronicConditions != null)
            {
                child.ChronicConditions = childDto.ChronicConditions;
            }
            if (childDto.Gender != null)
            {
                child.Gender = childDto.Gender;
            }

            _context.Entry(child).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChildExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Child not found"));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to edit child"));
            }

            var childDTO = child.ToGetChildDTO();
            return Ok(ApiResponse<GetChildDTO>.Success(childDTO));
        }

        // PUT: api/Children/5
        /// <summary>
        /// Update a child by ID (Admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Child ID mismatch
        /// - Child not found
        /// </remarks>
        /// <response code="204">Child updated</response>
        /// <response code="400">Child ID mismatch</response>
        /// <response code="404">Child not found</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChildAdmin(int id, EditChildDTO childDto)
        {
            var child = await _context.Children.FindAsync(id);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (id != child.ChildrenId)
            {
                return BadRequest(ApiResponse<object>.Error("Child ID mismatch"));
            }

            // Update child properties with non-null values from childDto
            if (childDto.FullName != null)
            {
                child.FullName = childDto.FullName;
            }
            if (childDto.Avatar != null)
            {
                child.Avatar = childDto.Avatar;
            }
            if (childDto.Dob.HasValue)
            {
                child.Dob = childDto.Dob.Value;
            }
            if (childDto.BloodType != null)
            {
                child.BloodType = childDto.BloodType;
            }
            if (childDto.Allergies != null)
            {
                child.Allergies = childDto.Allergies;
            }
            if (childDto.ChronicConditions != null)
            {
                child.ChronicConditions = childDto.ChronicConditions;
            }
            if (childDto.Gender != null)
            {
                child.Gender = childDto.Gender;
            }

            _context.Entry(child).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChildExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Child not found"));
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        // POST: api/Children/add
        /// <summary>
        /// Add a new child to the currently logged-in user (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="201">Child added</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="400">Bad request</response>
        [Authorize]
        [HttpPost("add")]
        public async Task<ActionResult<ApiResponse<GetChildDTO>>> AddChild(AddChildDTO childDto)
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

            var child = new Child
            {
                FullName = childDto.FullName,
                Avatar = childDto.Avatar,
                MemberId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                Dob = childDto.Dob,
                BloodType = childDto.BloodType,
                Allergies = childDto.Allergies,
                ChronicConditions = childDto.ChronicConditions,
                Gender = childDto.Gender
            };

            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            var childDTO = child.ToGetChildDTO();
            return CreatedAtAction(nameof(GetChildAdmin), new { id = child.ChildrenId }, ApiResponse<GetChildDTO>.Success(childDTO));
        }

        // POST: api/Children
        /// <summary>
        /// Add a new child (Admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Invalid data
        /// </remarks>
        /// <response code="200">Child added</response>
        /// <response code="400">Invalid data</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<GetChildDTO>>> PostChildAdmin(NewChildDTO newChildDto)
        {
            var child = new Child
            {
                FullName = newChildDto.FullName,
                Avatar = newChildDto.Avatar ?? null,
                MemberId = newChildDto.MemberId,
                CreatedAt = DateTime.UtcNow,
                Dob = newChildDto.Dob,
                BloodType = newChildDto.BloodType,
                Allergies = newChildDto.Allergies,
                ChronicConditions = newChildDto.ChronicConditions,
                Gender = newChildDto.Gender
            };

            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            var childDTO = child.ToGetChildDTO();
            return Ok(ApiResponse<GetChildDTO>.Success(childDTO));
        }





        // DELETE: api/Children/5
        /// <summary>
        /// Delete a child by ID (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Child not found
        /// - Unauthorized to delete this child
        /// </remarks>
        /// <response code="204">Child deleted</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Child not found</response>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChild(int id)
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

            var child = await _context.Children.FindAsync(id);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to delete this child"));
            }

            _context.Children.Remove(child);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        // DELETE: api/Children/5
        /// <summary>
        /// Delete a child by ID (Admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Child not found
        /// </remarks>
        /// <response code="204">Child deleted</response>
        /// <response code="404">Child not found</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> DeleteChildAdmin(int id)
        {
            var child = await _context.Children.FindAsync(id);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            _context.Children.Remove(child);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChildExists(int id)
        {
            return _context.Children.Any(e => e.ChildrenId == id);
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
