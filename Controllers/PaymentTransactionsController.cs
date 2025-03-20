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
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.DTOs.PaymentTransactionDTO;
using SWD392.Models;
using SWD392.Service;

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
        public async Task<IActionResult> GetPaymentHistory(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest(new { message = "Page number and page size must be greater than 0." });
            }

            var userIdHeader = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userIdHeader))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            int userId = int.Parse(userIdHeader);

            var query = from t in _context.PaymentTransactions
                        join p in _context.MembershipPackages on t.MembershipPackageId equals p.MembershipPackageId
                        where t.UserId == userId && t.PaymentId != "FREE"
                        select new PaymentHistoryDTO
                        {
                            PaymentId = t.PaymentId,
                            UserId = t.UserId,
                            Amount = t.Amount,
                            TransactionDate = t.TransactionDate,
                            Status = t.Status,
                            PreviousMembershipPackageName = t.PreviousMembershipPackageName,
                            MembershipPackage = new GetPackageUserHistoryDTO
                            {
                                MembershipPackageId = p.MembershipPackageId,
                                MembershipPackageName = p.MembershipPackageName,
                                Price = p.Price,
                                Status = p.Status,
                                ValidityPeriod = p.ValidityPeriod,
                                Permissions = p.Permissions.Select(perm => new PermissionDTO
                                {
                                    PermissionId = perm.PermissionId,
                                    PermissionName = perm.PermissionName,
                                    Description = perm.Description
                                }).ToList()
                            }
                        };

            int totalRecords = await query.CountAsync();
            int maxPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            bool hasNext = pageNumber < maxPages;

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

         

            var pagination = new Pagination(maxPages, hasNext, totalRecords);
            return Ok(ApiResponse<object>.Success(transactions, pagination));
        }


        [Authorize(Roles = "member")]
        [HttpPatch("Cancel")]
        public async Task<IActionResult> CancelPaymentTransaction([FromBody] CancelPaymentDTO request)
        {
            if (request == null || request.PaymentTransactionId <= 0)
            {
                return BadRequest(new { message = "Invalid PaymentTransactionId" });
            }

            // Tìm giao dịch theo ID
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == request.PaymentTransactionId);

            if (transaction == null)
            {
                return NotFound(new { message = "Payment transaction not found" });
            }

            // Cập nhật trạng thái thành 'cancel'
            transaction.Status = "cancel";
            _context.PaymentTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction canceled successfully" });
        }
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
