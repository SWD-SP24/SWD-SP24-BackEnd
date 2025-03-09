using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.VaccinationScheduleDTOs;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccinationSchedulesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VaccinationSchedulesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all vaccination schedules.
        /// </summary>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with a list of <see cref="VaccinationScheduleDTO"/> objects.
        /// </returns>
        /// <response code="200">Returns the list of vaccination schedules.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<VaccinationScheduleDTO>>>> GetVaccinationSchedules()
        {
            var vaccinationSchedules = await _context.VaccinationSchedules.Include(vs => vs.Vaccine).ToListAsync();
            var vaccinationScheduleDtos = vaccinationSchedules.Select(vs => vs.ToVaccinationScheduleDto()).ToList();
            return Ok(ApiResponse<object>.Success(vaccinationScheduleDtos));
        }

        /// <summary>
        /// Retrieves a list of all vaccination schedules for a specific vaccine.
        /// </summary>
        /// <param name="vaccineId">The ID of the vaccine to retrieve schedules for.</param>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with a list of <see cref="VaccinationScheduleDTO"/> objects.
        /// </returns>
        /// <response code="200">Returns the list of vaccination schedules for the specified vaccine.</response>
        /// <response code="404">If no vaccination schedules are found for the specified vaccine.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpGet("vaccine/{vaccineId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VaccinationScheduleDTO>>>> GetVaccinationSchedulesByVaccineId(int vaccineId)
        {
            var vaccinationSchedules = await _context.VaccinationSchedules
                .Where(vs => vs.VaccineId == vaccineId)
                .Include(vs => vs.Vaccine)
                .ToListAsync();

            if (!vaccinationSchedules.Any())
            {
                return NotFound(ApiResponse<object>.Error("No vaccination schedules found for the specified vaccine"));
            }

            var vaccinationScheduleDtos = vaccinationSchedules.Select(vs => vs.ToVaccinationScheduleDto()).ToList();
            return Ok(ApiResponse<object>.Success(vaccinationScheduleDtos));
        }

        /// <summary>
        /// Retrieves a specific vaccination schedule by its ID.
        /// </summary>
        /// <param name="id">The ID of the vaccination schedule to retrieve.</param>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with the <see cref="VaccinationScheduleDTO"/> object.
        /// </returns>
        /// <response code="200">Returns the vaccination schedule with the specified ID.</response>
        /// <response code="404">If the vaccination schedule with the specified ID is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<VaccinationScheduleDTO>>> GetVaccinationSchedule(int id)
        {
            var vaccinationSchedule = await _context.VaccinationSchedules.FindAsync(id);

            if (vaccinationSchedule == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccination schedule not found"));
            }

            var vaccinationScheduleDto = vaccinationSchedule.ToVaccinationScheduleDto();
            return Ok(ApiResponse<VaccinationScheduleDTO>.Success(vaccinationScheduleDto));
        }

        /// <summary>
        /// Updates an existing vaccination schedule.
        /// </summary>
        /// <param name="id">The ID of the vaccination schedule to update.</param>
        /// <param name="editVaccinationScheduleDto">The updated vaccination schedule data.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the operation.
        /// </returns>
        /// <response code="204">If the vaccination schedule was successfully updated.</response>
        /// <response code="400">If the ID in the URL does not match the ID in the provided data.</response>
        /// <response code="404">If the vaccination schedule with the specified ID is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVaccinationSchedule(int id, EditVaccinationScheduleDTO editVaccinationScheduleDto)
        {
            var vaccinationSchedule = await _context.VaccinationSchedules.FindAsync(id);
            if (vaccinationSchedule == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccination schedule not found"));
            }

            // Update only the fields that have values
            if (editVaccinationScheduleDto.VaccineId.HasValue)
            {
                vaccinationSchedule.VaccineId = editVaccinationScheduleDto.VaccineId;
            }
            if (editVaccinationScheduleDto.RecommendedAgeMonths.HasValue)
            {
                vaccinationSchedule.RecommendedAgeMonths = editVaccinationScheduleDto.RecommendedAgeMonths;
            }

            _context.Entry(vaccinationSchedule).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VaccinationScheduleExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Vaccination schedule not found"));
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Creates a new vaccination schedule.
        /// </summary>
        /// <param name="createVaccinationScheduleDto">The data for the new vaccination schedule.</param>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with the created <see cref="VaccinationScheduleDTO"/> object.
        /// </returns>
        /// <response code="201">Returns the newly created vaccination schedule.</response>
        /// <response code="400">If the provided data is invalid or the maximum number of schedules for the vaccine is exceeded.</response>
        /// <response code="500">If there is an internal server error.</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VaccinationScheduleDTO>>> PostVaccinationSchedule(CreateVaccinationScheduleDTO createVaccinationScheduleDto)
        {
            const int MaxSchedulesPerVaccine = 5; // Define the maximum number of schedules allowed per vaccine

            // Check if the number of schedules for the given vaccine exceeds the maximum allowed
            var existingSchedulesCount = await _context.VaccinationSchedules
                .CountAsync(vs => vs.VaccineId == createVaccinationScheduleDto.VaccineId);

            if (existingSchedulesCount >= MaxSchedulesPerVaccine)
            {
                return BadRequest(ApiResponse<object>.Error("The maximum number of schedules for this vaccine has been exceeded."));
            }

            var vaccinationSchedule = createVaccinationScheduleDto.ToVaccinationSchedule();
            _context.VaccinationSchedules.Add(vaccinationSchedule);
            await _context.SaveChangesAsync();

            var vaccinationScheduleDto = vaccinationSchedule.ToVaccinationScheduleDto();
            return CreatedAtAction("GetVaccinationSchedule", new { id = vaccinationSchedule.Id }, ApiResponse<VaccinationScheduleDTO>.Success(vaccinationScheduleDto));
        }

        /// <summary>
        /// Deletes a specific vaccination schedule by its ID.
        /// </summary>
        /// <param name="id">The ID of the vaccination schedule to delete.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the operation.
        /// </returns>
        /// <response code="204">If the vaccination schedule was successfully deleted.</response>
        /// <response code="404">If the vaccination schedule with the specified ID is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVaccinationSchedule(int id)
        {
            var vaccinationSchedule = await _context.VaccinationSchedules.FindAsync(id);
            if (vaccinationSchedule == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccination schedule not found"));
            }

            _context.VaccinationSchedules.Remove(vaccinationSchedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VaccinationScheduleExists(int id)
        {
            return _context.VaccinationSchedules.Any(e => e.Id == id);
        }
    }
}
