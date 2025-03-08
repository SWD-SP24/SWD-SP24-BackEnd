using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.ToothDTO;
using SWD392.Mapper;
using SWD392.Models;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeethController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeethController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all teeth.
        /// </summary>
        /// <returns>A list of <see cref="GetToothDTO"/> objects.</returns>
        /// <response code="200">Returns the list of teeth.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetToothDTO>>> GetTeeth()
        {
            return await _context.Teeth
                .Select(tooth => tooth.ToGetToothDTO())
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific tooth by ID.
        /// </summary>
        /// <param name="id">The ID of the tooth to retrieve.</param>
        /// <returns>A <see cref="GetToothDTO"/> object.</returns>
        /// <response code="200">Returns the tooth.</response>
        /// <response code="404">If the tooth is not found.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<GetToothDTO>> GetTooth(int id)
        {
            var tooth = await _context.Teeth.FindAsync(id);

            if (tooth == null)
            {
                return NotFound();
            }

            return tooth.ToGetToothDTO();
        }

        /// <summary>
        /// Updates a specific tooth by ID (Authorized only).
        /// </summary>
        /// <param name="id">The ID of the tooth to update.</param>
        /// <param name="editToothDto">The DTO containing the updated tooth data.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        /// <response code="204">Tooth updated successfully.</response>
        /// <response code="404">If the tooth is not found.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PutTooth(int id, EditToothDTO editToothDto)
        {
            var tooth = await _context.Teeth.FindAsync(id);
            if (tooth == null)
            {
                return NotFound();
            }

            tooth.NumberOfTeeth = editToothDto.NumberOfTeeth ?? tooth.NumberOfTeeth;
            if (editToothDto.TeethingPeriod.HasValue)
            {
                tooth.TeethingPeriod = editToothDto.TeethingPeriod.Value;
            }
            tooth.Name = editToothDto.Name ?? tooth.Name;

            _context.Entry(tooth).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ToothExists(id))
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

        /// <summary>
        /// Creates a new tooth (Authorized only).
        /// </summary>
        /// <param name="createToothDto">The DTO containing the new tooth data.</param>
        /// <returns>A <see cref="GetToothDTO"/> object.</returns>
        /// <response code="201">Tooth created successfully.</response>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<GetToothDTO>> PostTooth(CreateToothDTO createToothDto)
        {
            var tooth = new Tooth
            {
                NumberOfTeeth = createToothDto.NumberOfTeeth,
                TeethingPeriod = createToothDto.TeethingPeriod,
                Name = createToothDto.Name
            };

            _context.Teeth.Add(tooth);
            await _context.SaveChangesAsync();

            var getToothDto = tooth.ToGetToothDTO();

            return CreatedAtAction("GetTooth", new { id = tooth.Id }, getToothDto);
        }

        /// <summary>
        /// Deletes a specific tooth by ID (Authorized only).
        /// </summary>
        /// <param name="id">The ID of the tooth to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.</returns>
        /// <response code="204">Tooth deleted successfully.</response>
        /// <response code="404">If the tooth is not found.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTooth(int id)
        {
            var tooth = await _context.Teeth.FindAsync(id);
            if (tooth == null)
            {
                return NotFound();
            }

            _context.Teeth.Remove(tooth);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ToothExists(int id)
        {
            return _context.Teeth.Any(e => e.Id == id);
        }
    }
}
