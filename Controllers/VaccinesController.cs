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
using SWD392.DTOs.VaccinesDTO;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccinesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VaccinesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all vaccines.
        /// </summary>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with a list of <see cref="VaccineDTO"/> objects.
        /// </returns>
        /// <response code="200">Returns the list of vaccines.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<VaccineDTO>>>> GetVaccines(int pageNumber = 1, int pageSize = 999)
        {
            var totalVaccines = await _context.Vaccines.CountAsync();
            var vaccines = await _context.Vaccines
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vaccineDtos = vaccines.Select(v => v.ToVaccineDto()).ToList();
            var hasNext = (pageNumber * pageSize) < totalVaccines;
            var maxPages = (int)Math.Ceiling(totalVaccines / (double)pageSize);

            var pagination = new Pagination(maxPages, hasNext, totalVaccines);

            return Ok(ApiResponse<object>.Success(vaccineDtos, pagination));
        }

        /// <summary>
        /// Retrieves a list of vaccination schedules for a specific vaccine.
        /// </summary>
        /// <param name="vaccineId">The ID of the vaccine.</param>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with a list of <see cref="VaccinationScheduleDTO"/> objects.
        /// </returns>
        /// <response code="200">Returns the list of vaccination schedules for the specified vaccine.</response>
        /// <response code="404">If no vaccination schedules are found for the specified vaccine.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpGet("vaccine/{vaccineId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VaccinationScheduleDTO>>>> GetVaccinationSchedulesByVaccine(int vaccineId)
        {
            var vaccinationSchedules = await _context.VaccinationSchedules
                .Where(vs => vs.VaccineId == vaccineId)
                .ToListAsync();

            if (!vaccinationSchedules.Any())
            {
                return NotFound(ApiResponse<object>.Error("No vaccination schedules found for the specified vaccine"));
            }

            var vaccinationScheduleDtos = vaccinationSchedules.Select(vs => vs.ToVaccinationScheduleDto()).ToList();
            return Ok(ApiResponse<object>.Success(vaccinationScheduleDtos));
        }

        /// <summary>
        /// Retrieves a specific vaccine by its ID.
        /// </summary>
        /// <param name="id">The ID of the vaccine to retrieve.</param>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with the <see cref="VaccineDTO"/> object.
        /// </returns>
        /// <response code="200">Returns the vaccine with the specified ID.</response>
        /// <response code="404">If the vaccine with the specified ID is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<VaccineDTO>>> GetVaccine(int id)
        {
            var vaccine = await _context.Vaccines.FindAsync(id);

            if (vaccine == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccine not found"));
            }

            var vaccineDto = vaccine.ToVaccineDto();
            return Ok(ApiResponse<VaccineDTO>.Success(vaccineDto));
        }

        /// <summary>
        /// Updates an existing vaccine.
        /// </summary>
        /// <param name="id">The ID of the vaccine to update.</param>
        /// <param name="editVaccineDto">The updated vaccine data.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the operation.
        /// </returns>
        /// <response code="204">If the vaccine was successfully updated.</response>
        /// <response code="400">If the ID in the URL does not match the ID in the provided data.</response>
        /// <response code="404">If the vaccine with the specified ID is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVaccine(int id, EditVaccineDTO editVaccineDto)
        {
            var vaccine = await _context.Vaccines.FindAsync(id);
            if (vaccine == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccine not found"));
            }

            // Update only the fields that have values
            if (!string.IsNullOrEmpty(editVaccineDto.Name))
            {
                vaccine.Name = editVaccineDto.Name;
            }
            if (!string.IsNullOrEmpty(editVaccineDto.Description))
            {
                vaccine.Description = editVaccineDto.Description;
            }
            if (editVaccineDto.DosesRequired.HasValue)
            {
                vaccine.DosesRequired = editVaccineDto.DosesRequired;
            }

            _context.Entry(vaccine).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VaccineExists(id))
                {
                    return NotFound(ApiResponse<object>.Error("Vaccine not found"));
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        /// <summary>
        /// Creates a new vaccine.
        /// </summary>
        /// <param name="createVaccineDto">The data for the new vaccine.</param>
        /// <returns>
        /// An <see cref="ActionResult"/> containing an <see cref="ApiResponse{T}"/> with the created <see cref="VaccineDTO"/> object.
        /// </returns>
        /// <response code="201">Returns the newly created vaccine.</response>
        /// <response code="400">If the provided data is invalid.</response>
        /// <response code="500">If there is an internal server error.</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VaccineDTO>>> PostVaccine(CreateVaccineDTO createVaccineDto)
        {
            var vaccine = createVaccineDto.ToVaccine();
            _context.Vaccines.Add(vaccine);
            await _context.SaveChangesAsync();

            var vaccineDto = vaccine.ToVaccineDto();
            return CreatedAtAction("GetVaccine", new { id = vaccine.Id }, ApiResponse<VaccineDTO>.Success(vaccineDto));
        }

        /// <summary>
        /// Deletes a specific vaccine by its ID.
        /// </summary>
        /// <param name="id">The ID of the vaccine to delete.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the operation.
        /// </returns>
        /// <response code="204">If the vaccine was successfully deleted.</response>
        /// <response code="404">If the vaccine with the specified ID is not found.</response>
        /// <response code="500">If there is an internal server error.</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVaccine(int id)
        {
            var vaccine = await _context.Vaccines.FindAsync(id);
            if (vaccine == null)
            {
                return NotFound(ApiResponse<object>.Error("Vaccine not found"));
            }

            _context.Vaccines.Remove(vaccine);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VaccineExists(int id)
        {
            return _context.Vaccines.Any(e => e.Id == id);
        }
    }
}
