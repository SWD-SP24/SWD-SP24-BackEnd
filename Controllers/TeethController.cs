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

        // GET: api/Teeth
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetToothDTO>>> GetTeeth()
        {
            return await _context.Teeth
                .Select(tooth => tooth.ToGetToothDTO())
                .ToListAsync();
        }

        // GET: api/Teeth/5
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

        // PUT: api/Teeth/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
            tooth.TeethingPeriod = editToothDto.TeethingPeriod ?? tooth.TeethingPeriod;
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

        // POST: api/Teeth
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

        // DELETE: api/Teeth/5
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
