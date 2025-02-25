using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal.Api;
using SWD392.Data;
using SWD392.DTOs.PaymentTransactionDTO;
using SWD392.Models;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "member")]
        [HttpGet("history")]
        public IActionResult GetPaymentHistory()
        {
            // Lấy "id" từ HTTP context header (Authorization Token)
            var userIdHeader = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userIdHeader))
            {
                return Unauthorized("User ID not found in token.");
            }

            int userId = int.Parse(userIdHeader);

           
            var transactions = _context.PaymentTransactions
                .Where(t => t.UserId == userId && t.Status.ToLower() == "success" && t.PaymentId != "FREE") // Chỉ lấy status = "success"
                .Select(t => new PaymentHistoryDTO
                {
                    PaymentId = t.PaymentId,
                    UserId = t.UserId,
                    MembershipPackageId = t.MembershipPackageId,
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    
                })
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            return Ok(transactions);
        }

        /*// GET: api/PaymentTransactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentTransaction>> GetPaymentTransaction(int id)
        {
            var paymentTransaction = await _context.PaymentTransactions.FindAsync(id);

            if (paymentTransaction == null)
            {
                return NotFound();
            }

            return paymentTransaction;
        }

        // PUT: api/PaymentTransactions/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaymentTransaction(int id, PaymentTransaction paymentTransaction)
        {
            if (id != paymentTransaction.PaymentTransactionId)
            {
                return BadRequest();
            }

            _context.Entry(paymentTransaction).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentTransactionExists(id))
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

        // POST: api/PaymentTransactions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PaymentTransaction>> PostPaymentTransaction(PaymentTransaction paymentTransaction)
        {
            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPaymentTransaction", new { id = paymentTransaction.PaymentTransactionId }, paymentTransaction);
        }

        // DELETE: api/PaymentTransactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentTransaction(int id)
        {
            var paymentTransaction = await _context.PaymentTransactions.FindAsync(id);
            if (paymentTransaction == null)
            {
                return NotFound();
            }

            _context.PaymentTransactions.Remove(paymentTransaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PaymentTransactionExists(int id)
        {
            return _context.PaymentTransactions.Any(e => e.PaymentTransactionId == id);
        }*/
    }
}
