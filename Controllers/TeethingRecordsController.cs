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
using SWD392.DTOs.TeethingRecordsDTO;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeethingRecordsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeethingRecordsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of teething records for a specified child, optionally filtered by a time range. (Authorized and Doctor only)
        /// </summary>
        /// <param name="childId">The ID of the child whose teething records are to be retrieved.</param>
        /// <param name="startTime">The optional start time to filter the teething records in dd/MM/yyyy format.</param>
        /// <param name="endTime">The optional end time to filter the teething records in dd/MM/yyyy format.</param>
        /// <param name="pageNumber">The optional page number for pagination.</param>
        /// <param name="pageSize">The optional page size for pagination.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing a list of <see cref="TeethingRecordDTO"/> objects.</returns>
        /// <response code="200">Returns the list of teething records.</response>
        /// <response code="401">If the user is not authorized.</response>
        /// <response code="404">If the child is not found.</response>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TeethingRecordDTO>>>> GetTeethingRecords(
            [FromQuery] int childId,
            [FromQuery] string startTime = null,
            [FromQuery] string endTime = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 999)
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

            var child = await _context.Children.FindAsync(childId);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (child.MemberId != user.UserId && user.Role != "doctor")
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to view this child's teething records"));
            }

            var query = _context.TeethingRecords.AsQueryable();

            if (!string.IsNullOrEmpty(startTime) && DateTime.TryParseExact(startTime, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedStartTime))
            {
                query = query.Where(tr => tr.RecordTime >= parsedStartTime);
            }

            if (!string.IsNullOrEmpty(endTime) && DateTime.TryParseExact(endTime, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedEndTime))
            {
                query = query.Where(tr => tr.RecordTime <= parsedEndTime);
            }

            query = query.Where(tr => tr.ChildId == childId);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var teethingRecords = await query
                .Include(tr => tr.Tooth) // Include the related Tooth 
                .OrderByDescending(tr => tr.RecordTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var teethingRecordDtos = teethingRecords.Select(tr => tr.ToTeethingRecordDto());

            var pagination = new Pagination(totalPages, pageNumber < totalPages, totalItems);

            return Ok(ApiResponse<IEnumerable<TeethingRecordDTO>>.Success(teethingRecordDtos, pagination));
        }

        /// <summary>
        /// Retrieves a specific teething record by ID (Authorized and Doctor only)
        /// </summary>
        /// <param name="id">The ID of the teething record to retrieve.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the <see cref="TeethingRecordDTO"/> object.</returns>
        /// <response code="200">Returns the teething record.</response>
        /// <response code="401">Unauthorized if the user is not authorized to view the teething record.</response>
        /// <response code="404">Not found if the teething record does not exist.</response>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TeethingRecordDTO>>> GetTeethingRecord(int id)
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

            var teethingRecord = await _context.TeethingRecords
                .Include(tr => tr.Child)
                .Include(tr => tr.Tooth)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (teethingRecord == null)
            {
                return NotFound(ApiResponse<object>.Error("Teething record not found"));
            }

            if (teethingRecord.Child.MemberId != user.UserId && user.Role != "doctor")
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to view this teething record"));
            }

            var teethingRecordDto = teethingRecord.ToTeethingRecordDto();
            return Ok(ApiResponse<TeethingRecordDTO>.Success(teethingRecordDto));
        }

        /// <summary>
        /// Updates a teething record (Authorized only)
        /// </summary>
        /// <param name="id">The ID of the teething record to update.</param>
        /// <param name="teethingRecordDto">The DTO containing the updated teething record data.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        /// <response code="200">Teething record updated successfully.</response>
        /// <response code="400">Bad request if the ID does not match the teething record ID.</response>
        /// <response code="401">Unauthorized if the user is not authorized to edit the teething record.</response>
        /// <response code="404">Not found if the teething record does not exist.</response>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeethingRecord(int id, EditTeethingRecordDTO teethingRecordDto)
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

            var teethingRecord = await _context.TeethingRecords
                .Include(tr => tr.Child)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (teethingRecord == null)
            {
                return NotFound(ApiResponse<object>.Error("Teething record not found"));
            }

            if (teethingRecord.Child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to edit this teething record"));
            }

            var tooth = await _context.Teeth.FirstOrDefaultAsync(t => t.Id == teethingRecord.ToothId);
            if (tooth == null)
            {
                return NotFound(ApiResponse<object>.Error("Tooth not found"));
            }

            // Update teething record properties with non-null values from teethingRecordDto
            teethingRecord.ToothId = tooth.Id;
            if (!string.IsNullOrEmpty(teethingRecordDto.EruptionDate) && DateTime.TryParseExact(teethingRecordDto.EruptionDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedEruptionDate))
            {
                teethingRecord.EruptionDate = parsedEruptionDate;
            }

            if (!string.IsNullOrEmpty(teethingRecordDto.RecordTime) && DateTime.TryParseExact(teethingRecordDto.RecordTime, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedRecordTime))
            {
                teethingRecord.RecordTime = parsedRecordTime;
            }

            if (!string.IsNullOrEmpty(teethingRecordDto.Note))
            {
                teethingRecord.Note = teethingRecordDto.Note;
            }

            _context.Entry(teethingRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeethingRecordExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Teething record not found"));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to edit teething record"));
            }

            var teethingRecordDTO = teethingRecord.ToTeethingRecordDto();
            return Ok(ApiResponse<TeethingRecordDTO>.Success(teethingRecordDTO));
        }



        /// <summary>
        /// Creates a new teething record (Authorized only)
        /// </summary>
        /// <param name="teethingRecordDto">The DTO containing the new teething record data.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> containing the created <see cref="TeethingRecordDTO"/> object.</returns>
        /// <response code="201">Teething record created successfully.</response>
        /// <response code="401">Unauthorized if the user is not authorized to add the teething record.</response>
        /// <response code="404">Not found if the child does not exist.</response>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TeethingRecordDTO>>> PostTeethingRecord(CreateTeethingRecordDTO teethingRecordDto)
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

            var child = await _context.Children.FindAsync(teethingRecordDto.ChildId);
            if (child == null)
            {
                return NotFound(ApiResponse<object>.Error("Child not found"));
            }

            if (child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to add teething record for this child"));
            }

            var tooth = await _context.Teeth.FirstOrDefaultAsync(t => t.Id == teethingRecordDto.ToothId);
            if (tooth == null)
            {
                return NotFound(ApiResponse<object>.Error("Tooth not found"));
            }

            DateTime? parsedEruptionDate = null;
            DateTime? parsedRecordTime = null;

            if (!string.IsNullOrEmpty(teethingRecordDto.EruptionDate) && DateTime.TryParseExact(teethingRecordDto.EruptionDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime eruptionDate))
            {
                parsedEruptionDate = eruptionDate;
            }

            if (!string.IsNullOrEmpty(teethingRecordDto.RecordTime) && DateTime.TryParseExact(teethingRecordDto.RecordTime, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime recordTime))
            {
                parsedRecordTime = recordTime;
            }

            var teethingRecord = new TeethingRecord
            {
                ChildId = teethingRecordDto.ChildId,
                ToothId = tooth.Id,
                EruptionDate = parsedEruptionDate,
                RecordTime = parsedRecordTime,
                Note = teethingRecordDto.Note
            };

            _context.TeethingRecords.Add(teethingRecord);
            await _context.SaveChangesAsync();

            var teethingRecordDTO = teethingRecord.ToTeethingRecordDto();
            return CreatedAtAction("GetTeethingRecord", new { id = teethingRecord.Id }, ApiResponse<TeethingRecordDTO>.Success(teethingRecordDTO));
        }

        /// <summary>
        /// Deletes a specific teething record by ID (Authorized only)
        /// </summary>
        /// <param name="id">The ID of the teething record to delete.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> indicating the result of the operation.</returns>
        /// <response code="204">Teething record deleted successfully.</response>
        /// <response code="401">Unauthorized if the user is not authorized to delete the teething record.</response>
        /// <response code="404">Not found if the teething record does not exist.</response>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteTeethingRecord(int id)
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

            var teethingRecord = await _context.TeethingRecords
                .Include(tr => tr.Child)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (teethingRecord == null)
            {
                return NotFound(ApiResponse<object>.Error("Teething record not found"));
            }

            if (teethingRecord.Child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("Unauthorized to delete this teething record"));
            }

            _context.TeethingRecords.Remove(teethingRecord);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeethingRecordExists(int id)
        {
            return _context.TeethingRecords.Any(e => e.Id == id);
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
