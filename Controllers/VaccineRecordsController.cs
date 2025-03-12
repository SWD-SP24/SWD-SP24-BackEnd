using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.VaccineRecordDTO;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccineRecordsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VaccineRecordsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/VaccineRecords
        /// <summary>
        /// Get all vaccine records of the currently logged-in user's children (Authorized and Doctor only)
        /// </summary>
        /// <param name="childId">Optional child ID to filter the vaccine records</param>
        /// <param name="vaccineId">Optional vaccine ID to filter the vaccine records</param>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">Vaccine records retrieved</response>
        /// <response code="401">Unauthorized</response>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<VaccineRecordDto>>>> GetVaccineRecords(int? childId = null, int? vaccineId = null)
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

            var query = _context.VaccineRecords
                .Include(vr => vr.Child)
                .Include(vr => vr.Vaccine)
                .AsQueryable();

            if (childId.HasValue)
            {
                var child = await _context.Children.FindAsync(childId.Value);
                if (child == null || child.MemberId != user.UserId && user.Role != "doctor" && user.Role != "admin")
                {
                    return Unauthorized(ApiResponse<object>.Error("You do not have access to this child"));
                }
                query = query.Where(vr => vr.ChildId == childId.Value);
            }

            if (vaccineId.HasValue)
            {
                query = query.Where(vr => vr.VaccineId == vaccineId.Value);
            }

            var vaccineRecords = await query.ToListAsync();
            var vaccineRecordDtos = vaccineRecords.Select(VaccineRecordMapper.ToDto).ToList();

            return Ok(ApiResponse<IEnumerable<VaccineRecordDto>>.Success(vaccineRecordDtos));
        }

        /// <summary>
        /// Get the next vaccination date for a specific child and vaccine (Authorized and Doctor only)
        /// </summary>
        /// <param name="childId">The ID of the child</param>
        /// <param name="vaccineId">The ID of the vaccine</param>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Child not found
        /// - Vaccine not found
        /// - Unauthorized access to the child
        /// </remarks>
        /// <response code="200">Next vaccination date retrieved</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Child or vaccine not found</response>
        [Authorize]
        [HttpGet("next-date")]
        public async Task<ActionResult<ApiResponse<string>>> GetNextDate(int childId, int vaccineId)
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
            if (child == null || child.MemberId != user.UserId && user.Role != "doctor")
            {
                return Unauthorized(ApiResponse<object>.Error("You do not have access to this child"));
            }

            var vaccine = await _context.Vaccines.FindAsync(vaccineId);
            if (vaccine == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccine not found"));
            }

            var lastVaccineRecord = await _context.VaccineRecords
                .Where(vr => vr.ChildId == childId && vr.VaccineId == vaccineId)
                .OrderByDescending(vr => vr.AdministeredDate)
                .FirstOrDefaultAsync();

            if (lastVaccineRecord == null)
            {
                // No vaccination records found, calculate the recommended date based on the child's birthdate
                var vaccinationSchedule = await _context.VaccinationSchedules
                    .Where(vs => vs.VaccineId == vaccineId)
                    .OrderBy(vs => vs.RecommendedAgeMonths)
                    .FirstOrDefaultAsync();

                if (vaccinationSchedule == null)
                {
                    return NotFound(ApiResponse<object>.Error("No vaccination schedule found for this vaccine"));
                }

                if (child.Dob == null)
                {
                    return BadRequest(ApiResponse<object>.Error("Child's date of birth is not available"));
                }

                if (!vaccinationSchedule.RecommendedAgeMonths.HasValue)
                {
                    return Ok(ApiResponse<string>.Success("No time recommendation available"));
                }

                var recommendedDate = child.Dob.Value.AddMonths(vaccinationSchedule.RecommendedAgeMonths.Value);
                return Ok(ApiResponse<string>.Success(recommendedDate.ToString("dd/MM/yyyy")));
            }

            var nextDoseDate = lastVaccineRecord.NextDoseDate;
            if (nextDoseDate == null)
            {
                return Ok(ApiResponse<string>.Success("No further doses required"));
            }

            return Ok(ApiResponse<string>.Success(nextDoseDate.Value.ToString("dd/MM/yyyy")));
        }




        /// <summary>
        /// Get a specific vaccine record by ID (Authorized and Doctor only)
        /// </summary>
        /// <param name="id">The ID of the vaccine record</param>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Vaccine record not found
        /// - Unauthorized access to the vaccine record
        /// </remarks>
        /// <response code="200">Vaccine record retrieved</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vaccine record not found</response>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<VaccineRecordDto>>> GetVaccineRecord(int id)
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

            var vaccineRecord = await _context.VaccineRecords
                .Include(vr => vr.Child)
                .Include(vr => vr.Vaccine)
                .FirstOrDefaultAsync(vr => vr.Id == id);

            if (vaccineRecord == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccine record not found"));
            }

            if (vaccineRecord.Child.MemberId != user.UserId && user.Role != "doctor")
            {
                return Unauthorized(ApiResponse<object>.Error("You do not have access to this record"));
            }

            var vaccineRecordDto = VaccineRecordMapper.ToDto(vaccineRecord);

            return Ok(ApiResponse<VaccineRecordDto>.Success(vaccineRecordDto));
        }

        /// <summary>
        /// Update a specific vaccine record by ID (Authorized only)
        /// </summary>
        /// <param name="id">The ID of the vaccine record</param>
        /// <param name="updateDto">The DTO containing the updated vaccine record data</param>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Vaccine record not found
        /// - Unauthorized access to the vaccine record
        /// - Invalid data
        /// </remarks>
        /// <response code="204">Vaccine record updated</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vaccine record not found</response>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVaccineRecord(int id, UpdateVaccineRecordDto updateDto)
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

            var vaccineRecord = await _context.VaccineRecords
                .Include(vr => vr.Child)
                .FirstOrDefaultAsync(vr => vr.Id == id);

            if (vaccineRecord == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccine record not found"));
            }

            if (vaccineRecord.Child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("You do not have access to this record"));
            }

            bool administeredDateChanged = false;
            if (!string.IsNullOrEmpty(updateDto.AdministeredDate))
            {
                var newAdministeredDate = DateTime.ParseExact(updateDto.AdministeredDate, "dd/MM/yyyy", null);
                vaccineRecord.AdministeredDate = newAdministeredDate;
                administeredDateChanged = true;
            }

            DateTime? nextDoseDate = null;
            if (vaccineRecord.VaccineId != 0 && administeredDateChanged)
            {
                var vaccine = await _context.Vaccines.FindAsync(vaccineRecord.VaccineId);
                if (vaccine != null && vaccine.DosesRequired.HasValue && vaccineRecord.Dose == vaccine.DosesRequired.Value - 1)
                {
                    var vaccinationSchedules = await _context.VaccinationSchedules
                        .Where(vs => vs.VaccineId == vaccineRecord.VaccineId)
                        .OrderBy(vs => vs.RecommendedAgeMonths)
                        .ToListAsync();

                    var currentDoseSchedule = vaccinationSchedules.ElementAtOrDefault(vaccineRecord.Dose.Value - 1);
                    var nextDoseSchedule = vaccinationSchedules.ElementAtOrDefault(vaccineRecord.Dose.Value);

                    if (currentDoseSchedule != null && nextDoseSchedule != null && !string.IsNullOrEmpty(updateDto.AdministeredDate))
                    {
                        var intervalMonths = nextDoseSchedule.RecommendedAgeMonths - currentDoseSchedule.RecommendedAgeMonths;
                        nextDoseDate = DateTime.ParseExact(updateDto.AdministeredDate, "dd/MM/yyyy", null).AddMonths(intervalMonths!.Value);
                    }
                }
            }

            if (administeredDateChanged)
            {
                vaccineRecord.NextDoseDate = nextDoseDate;
            }

            _context.Entry(vaccineRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VaccineRecordExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Vaccine record not found"));
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Create a new vaccine record (Authorized only)
        /// </summary>
        /// <param name="createDto">The DTO containing the new vaccine record data</param>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Invalid data
        /// </remarks>
        /// <response code="201">Vaccine record created</response>
        /// <response code="400">Invalid data</response>
        /// <response code="401">Unauthorized</response>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VaccineRecordDto>>> PostVaccineRecord(CreateVaccineRecordDto createDto)
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

            var child = await _context.Children.FindAsync(createDto.ChildId);
            if (child == null || child.MemberId != user.UserId)
            {
                return Unauthorized(ApiResponse<object>.Error("You do not have access to this child"));
            }

            var vaccine = await _context.Vaccines.FindAsync(createDto.VaccineId);
            if (vaccine == null)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid vaccine ID"));
            }

            // Check if the dose is outside of the valid range
            if (createDto.Dose < 1 || (vaccine.DosesRequired.HasValue && createDto.Dose > vaccine.DosesRequired.Value))
            {
                return BadRequest(ApiResponse<object>.Error("Invalid dose number"));
            }

            // Check for uniqueness
            var existingRecord = await _context.VaccineRecords
                .FirstOrDefaultAsync(vr => vr.ChildId == createDto.ChildId && vr.VaccineId == createDto.VaccineId && vr.Dose == createDto.Dose);
            if (existingRecord != null)
            {
                return BadRequest(ApiResponse<object>.Error("A vaccine record with the same vaccine, dose number, and child already exists"));
            }

            DateTime? nextDoseDate = null;
            if (vaccine.DosesRequired.HasValue && createDto.Dose <= vaccine.DosesRequired.Value)
            {
                var vaccinationSchedules = await _context.VaccinationSchedules
                    .Where(vs => vs.VaccineId == createDto.VaccineId)
                    .OrderBy(vs => vs.RecommendedAgeMonths)
                    .ToListAsync();

                var currentDoseSchedule = vaccinationSchedules.ElementAtOrDefault((Index)(createDto.Dose - 1));
                var nextDoseSchedule = vaccinationSchedules.ElementAtOrDefault((Index)(createDto.Dose));

                if (currentDoseSchedule != null && nextDoseSchedule != null)
                {
                    var intervalMonths = nextDoseSchedule.RecommendedAgeMonths - currentDoseSchedule.RecommendedAgeMonths;
                    nextDoseDate = DateTime.ParseExact(createDto.AdministeredDate, "dd/MM/yyyy", null).AddMonths(intervalMonths!.Value);
                }
            } 
            else
            {
                nextDoseDate = null;
            }

            var vaccineRecord = new VaccineRecord
            {
                ChildId = createDto.ChildId,
                VaccineId = createDto.VaccineId,
                AdministeredDate = DateTime.ParseExact(createDto.AdministeredDate, "dd/MM/yyyy", null),
                Dose = createDto.Dose,
                NextDoseDate = nextDoseDate
            };

            _context.VaccineRecords.Add(vaccineRecord);
            await _context.SaveChangesAsync();

            var vaccineRecordDto = VaccineRecordMapper.ToDto(vaccineRecord);

            return CreatedAtAction("GetVaccineRecord", new { id = vaccineRecord.Id }, ApiResponse<VaccineRecordDto>.Success(vaccineRecordDto));
        }


        // DELETE: api/VaccineRecords/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVaccineRecord(int id)
        {
            var vaccineRecord = await _context.VaccineRecords.FindAsync(id);
            if (vaccineRecord == null)
            {
                return NotFound();
            }

            _context.VaccineRecords.Remove(vaccineRecord);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VaccineRecordExists(int id)
        {
            return _context.VaccineRecords.Any(e => e.Id == id);
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
