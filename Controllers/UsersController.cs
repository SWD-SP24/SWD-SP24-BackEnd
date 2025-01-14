using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.UserDTO;
using SWD392.Mapper;
using SWD392.Models;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FirebaseService _authentication;
        private readonly TokenService _tokenService;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _authentication = new FirebaseService();
            _tokenService = new TokenService(configuration);
        }

        // POST: api/Users/
        [HttpPost("register")]
        public async Task<ActionResult<User>> RegisterUser(RegisterUserDTO userDTO)
        {
            if (_context.Users.Any(_context => _context.Email == userDTO.Email)) { return BadRequest("Email already exists"); }
            string uid = "";
            try
            {
                uid = await _authentication.RegisterAsync(userDTO.Email, userDTO.Password);
            }
            catch (Exception)
            {
                return BadRequest("Firebase failed");
            }

            var newUser = userDTO.ToUser(uid);

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                await _authentication.DeleteAsync(uid);
                return BadRequest("Register failed");
            }
            //_context.Users.Add(newUser);
            //await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = newUser.UserId }, newUser);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> LoginUser(LoginUserDTO userDTO)
        {
            // TODO: support login with multiple methods
            // TODO: Authentication check for all endpoints
            // TODO: Confirm email
            // TODO: Hash password
            var loginUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == userDTO.Email);
            if (loginUser == null) { return BadRequest("Account does not exist"); }

            if (loginUser.PasswordHash != userDTO.Password) { return BadRequest("Password is incorrect"); }

            string token = "";
            try
            {
                token = _tokenService.CreateUserToken(loginUser);
            }
            catch (Exception)
            {
                return BadRequest("Unable to create JWT Token");
            }

            var loginResponse = loginUser.ToLoginResponseDTO(token);

            return loginResponse;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetUserDTO>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDTOs = users.Select(user => user.ToGetAllUserDTO()).ToList();
            return Ok(userDTOs);

        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _authentication.DeleteAsync(user.Uid);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
