using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.FaqDTO;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaqsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FaqsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Faqs
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Faq>>> GetFaqs()
        {
            return await _context.Faqs.ToListAsync();
        }

        // GET: api/Faqs/5
       
        [HttpGet("{id}")]
        public async Task<ActionResult<Faq>> GetFaq(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);

            if (faq == null)
            {
                return NotFound();
            }

            return faq;
        }

        // PUT: api/Faqs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] CreateFaqDTO dto)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null)
            {
                return NotFound(new { message = "FAQ not found" });
            }

            if (!string.IsNullOrEmpty(dto.Question))
            {
                faq.Question = dto.Question;
            }

            if (!string.IsNullOrEmpty(dto.Answer))
            {
                faq.Answer = dto.Answer;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "FAQ updated successfully",
                data = new
                {
                    Question = faq.Question,
                    Answer = faq.Answer,
                    
                }
            });
        }

        // POST: api/Faqs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult> CreateFaq([FromBody] CreateFaqDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Question))
            {
                return BadRequest(new { status = "error", message = "Question is required." });
            }

            if (string.IsNullOrEmpty(dto.Answer))
            {
                return BadRequest(new { status = "error", message = "Answer is required." });
            }
            var newFAQ = new Faq
            {
                Question = dto.Question,
                Answer = dto.Answer
            };

            _context.Faqs.Add(newFAQ);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFaq), new { id = newFAQ.Id }, newFAQ);
        }


        // DELETE: api/Faqs/5
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFaq(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq == null)
            {
                return NotFound();
            }

            _context.Faqs.Remove(faq);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FaqExists(int id)
        {
            return _context.Faqs.Any(e => e.Id == id);
        }
    }
}
