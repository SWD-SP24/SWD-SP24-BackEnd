using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyMembershipPackage : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PayPalController _paypal;
        public BuyMembershipPackage(AppDbContext context, PayPalController paypal)
        {
            _context = context;
            _paypal = paypal;   
        }
        // GET: api/<BuyMembershipPackage>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<BuyMembershipPackage>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<BuyMembershipPackage>
        [HttpPost]
        //[Authorize(Roles = "member")]
        public async Task<IActionResult> Post([FromBody] int idPackage)
        {
            var checkPackage = await _context.MembershipPackages.FirstOrDefaultAsync(x => x.MembershipPackageId.Equals(idPackage));
            if (checkPackage == null)
            {
                throw new Exception("deo co package");
            }
            var link = await _paypal.CreatePayment(checkPackage);

            return Ok(new {link});
        }

        // PUT api/<BuyMembershipPackage>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<BuyMembershipPackage>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
