using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Common;
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
        private readonly EmailService _emailService;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _authentication = new FirebaseService();
            _tokenService = new TokenService(configuration);
            var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
            _emailService = new EmailService(connectionString ?? "");
        }

        // POST: api/Users/
        [HttpPost("register")]
        public async Task<ActionResult<GetUserDTO>> RegisterUser(RegisterUserDTO userDTO)
        {
            if (_context.Users.Any(_context => _context.Email == userDTO.Email)) 
            { 
                //return BadRequest(new {status = 1000, message = "Email already exists."}); 
                return BadRequest(ApiResponse<object>.Error("Email already exists.")); 
            } else if (_context.Users.Any(_context => _context.PhoneNumber == userDTO.PhoneNumber))
            {
                return BadRequest(ApiResponse<object>.Error("Phone number already exists."));
            }
            string uid = "";
            try
            {
                uid = await _authentication.RegisterAsync(userDTO.Email, userDTO.Password);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Fail to create account (FB)"));
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
                return BadRequest(ApiResponse<object>.Error("Fail to create account (DB)"));
            }
            //_context.Users.Add(newUser);
            //await _context.SaveChangesAsync();

            _ = Task.Run(() =>
            {
                try
                {
                    // Send account confirmation email
                    _emailService.SendAccountConfirmationEmail(newUser.Email, "Nice");
                }
                catch (Exception)
                {
                    // TODO: Find a way to handle this error
                }
            });

            return Ok(ApiResponse<object>.Success(newUser.ToGetUserDTO()));
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> LoginUser(LoginUserDTO userDTO)
        {
            // TODO: support login with multiple methods
            // TODO: Confirm email
            // TODO: Reset password
            
            var loginUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == userDTO.Email);
            if (loginUser == null) { return BadRequest(ApiResponse<object>.Error("Account does not exist" ) ); }

            if (loginUser.PasswordHash != userDTO.Password) { return BadRequest(ApiResponse<object>.Error("Password is incorrect" )); }

            string token = "";
            try
            {
                token = _tokenService.CreateUserToken(loginUser);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to create JWT Token"));
            }

            var loginResponse = loginUser.ToLoginResponseDTO(token);

            return Ok(ApiResponse<object>.Success(loginResponse ));
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetUserDTO>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDTOs = users.Select(user => user.ToGetUserDTO()).ToList();
            return Ok(ApiResponse<object>.Success(userDTOs));

        }

        [HttpGet("self")]
        public async Task<ActionResult<GetUserDTO>> GetSelf()
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key")); //or whatever

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(authHeader);

            // Check if token has expired
            if (token.ValidTo < DateTime.UtcNow)
                return Unauthorized(ApiResponse<object>.Error("JWT token has expired" ));

            var rawId = token.Claims.First(claim => claim.Type == "id").Value;

            var id = int.Parse(rawId);

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return Unauthorized(ApiResponse<object>.Error("Invalid JWT key"));
            }

            return Ok(ApiResponse<object>.Success(user.ToGetUserDTO()));
        }


        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GetUserDTO>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist" ));
            }

            return Ok(ApiResponse<object>.Success(user.ToGetUserDTO()));
        }

        [HttpPut]
        public async Task<IActionResult> EditSelf(EditUserDTO userDto)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key" )); //or whatever

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(authHeader);

            // Check if token has expired
            if (token.ValidTo < DateTime.UtcNow)
                return Unauthorized(ApiResponse<object>.Error("JWT token has expired" ));

            var rawId = token.Claims.First(claim => claim.Type == "id").Value;

            var id = int.Parse(rawId);

            if (UserExists(id) == false) { return Unauthorized(ApiResponse<object>.Error("Invalid JWT key" )); }

            return await PutUser(id, userDto);
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, EditUserDTO userDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist" ));
            }

            {
                // Update user properties with non-null values from userDto
                if (userDto.PhoneNumber != null)
                {
                    user.PhoneNumber = userDto.PhoneNumber;
                }
                if (userDto.Password != null)
                {
                    user.PasswordHash = userDto.Password;
                }
                if (userDto.FullName != null)
                {
                    user.FullName = userDto.FullName;
                }
                if (userDto.Avatar != null)
                {
                    user.Avatar = userDto.Avatar;
                }
                if (userDto.Role != null)
                {
                    user.Role = userDto.Role;
                }
                if (userDto.Status != null)
                {
                    user.Status = userDto.Status;
                }
                if (userDto.MembershipPackageId != null)
                {
                    user.MembershipPackageId = userDto.MembershipPackageId;
                }
                if (userDto.Uid != null)
                {
                    user.Uid = userDto.Uid;
                }
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
                    return NotFound(ApiResponse<object>.Error("Account does not exist" ));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to edit user" ));
            }

            return Ok(ApiResponse<object>.Success(user.ToGetUserDTO()));
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist" ));
            }

            await _authentication.DeleteAsync(user.Uid);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Success("", message: "Delete successful" ));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}