using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using OtpNet;
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
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _authentication = new FirebaseService();
            _tokenService = new TokenService(configuration);
            var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
            _emailService = new EmailService(connectionString ?? "", configuration);
            _configuration = configuration;
        }

        // POST: api/Users/
        /// <summary>
        /// Register user
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Email already exists.
        /// - Fail to create account (FB)  Check for valid email.
        /// - Fail to create account (DB)
        /// </remarks>
        /// <response code="200">User register</response>
        [HttpPost("register")]
        public async Task<ActionResult<GetUserDTO>> RegisterUser(RegisterUserDTO userDTO)
        {
            if (_context.Users.Any(_context => _context.Email == userDTO.Email)) 
            { 
                //return BadRequest(new {status = 1000, message = "Email already exists."}); 
                return BadRequest(ApiResponse<object>.Error("Email already exists.")); 
            } 
            //else if (_context.Users.Any(_context => _context.PhoneNumber == userDTO.PhoneNumber))
            //{
            //    return BadRequest(ApiResponse<object>.Error("Phone number already exists."));
            //}
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
                    var verifyToken = _tokenService.CreateVerifyEmailToken(uid);
                    // Send account confirmation email
                    _emailService.SendAccountConfirmationEmail(newUser.Email, verifyToken);

                }
                catch (Exception)
                {
                    // TODO: Find a way to handle this error
                }
            });

            string token = "";
            try
            {
                token = _tokenService.CreateUserToken(newUser);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to create JWT Token"));
            }

            return Ok(ApiResponse<object>.Success(newUser.ToLoginResponseDTO(token)));
        }



        /// <summary>
        /// Login user
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Password is incorrect
        /// - Unable to create JWT Token
        /// </remarks>
        /// <response code="200">Logged in</response>

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
        /// <summary>
        /// Get user list
        /// </summary>
        /// <remarks>
        /// Errors:
        /// </remarks>
        /// <response code="200">Users retrieved</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetUserDTO>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDTOs = users.Select(user => user.ToGetUserDTO()).ToList();
            return Ok(ApiResponse<object>.Success(userDTOs));

        }

        /// <summary>
        /// Get currently logged in user
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">User retrieved</response>

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
        /// <summary>
        /// Get user with ID
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// </remarks>
        /// <response code="200">User retrieved</response>
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

        // GET: api/Users/5
        /// <summary>
        /// Edit currently logged in user
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">self edited</response>
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
        /// <summary>
        /// Update user with specified ID
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Unable to edit user
        /// </remarks>
        /// <response code="200">User edited</response>
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

        /// <summary>
        /// Delete user with specified ID <b>Broken do not use!!!!</b>
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Unable to delete user (SQL)
        /// - Unable to delete user (Firebase)
        /// </remarks>
        /// <response code="200">Delete successful</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist" ));
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to delete user (SQL)"));
            }

            try
            {
                await _authentication.DeleteAsync(user.Uid);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to delete user (Firebase)"));
            }

            return Ok(ApiResponse<object>.Success("", message: "Delete successful" ));
        }

        /// <summary>
        /// Check if user with id exists
        /// </summary>
        /// <remarks>
        /// Errors:
        /// </remarks>
        /// <response code="200">successful</response>

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        /// <summary>
        /// Verify user email !!!Use from email only!!!
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Token has expired
        /// - Invalid token
        /// - User not found
        /// </remarks>
        /// <response code="200">Email verified</response>
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            string uid;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    return BadRequest(ApiResponse<object>.Error("Token has expired"));
                }

                uid = jwtToken.Claims.First(claim => claim.Type == "nameid").Value;
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid token"));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Uid == uid);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("User not found"));
            }

            user.EmailActivation = "activated";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Success("Email verified"));
        }

        // POST: api/Users/change-password
        /// <summary>
        /// Change user password
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Incorrect old password
        /// - Unable to change password
        /// </remarks>
        /// <response code="200">Password changed</response>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO changePasswordDTO)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(authHeader);

            // Check if token has expired
            if (token.ValidTo < DateTime.UtcNow)
                return Unauthorized(ApiResponse<object>.Error("JWT token has expired"));

            var rawId = token.Claims.First(claim => claim.Type == "id").Value;
            var id = int.Parse(rawId);

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Error("Invalid JWT key"));

            if (user.PasswordHash != changePasswordDTO.OldPassword)
                return BadRequest(ApiResponse<object>.Error("Incorrect old password"));

            user.PasswordHash = changePasswordDTO.NewPassword;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to change password"));
            }

            return Ok(ApiResponse<object>.Success("Password changed"));
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Email does not exist
        /// </remarks>
        /// <response code="200">Password reset code sent to email</response>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return BadRequest(ApiResponse<object>.Error("Email does not exist"));
            }

            // Derive a per-user secret from the JWT secret and user identifier
            var jwtSecret = _configuration["JWT:SigningKey"];
            var userSecret = $"{jwtSecret}-{user.UserId}";
            var totp = new Totp(Encoding.UTF8.GetBytes(userSecret), step: 3600); // 1 hour time step

            // Compute an 8-digit TOTP code
            var code = totp.ComputeTotp();

            // Send the code to the user's email

            _ = Task.Run(() =>
            {
                try
                {
                    // Send password reset email
                    _emailService.SendPasswordRecoveryEmail(user.Email, code);

                }
                catch (Exception)
                {
                    // TODO: Find a way to handle this error
                }
            });

            return Ok(ApiResponse<object>.Success("Password reset code sent to email"));
        }

        /// <summary>
        /// Validate password reset code
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Email does not exist
        /// - Invalid or expired code
        /// </remarks>
        /// <response code="200">Code validated successfully</response>
        [HttpPost("validate-reset-code")]
        public async Task<IActionResult> ValidateResetCode([FromBody] ValidateResetCodeDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return BadRequest(ApiResponse<object>.Error("Email does not exist"));
            }

            // Derive a per-user secret from the JWT secret and user identifier
            var jwtSecret = _configuration["JWT:SigningKey"];
            var userSecret = $"{jwtSecret}-{user.UserId}";
            var totp = new Totp(Encoding.UTF8.GetBytes(userSecret), step: 3600); // 1 hour time step

            // Validate the submitted code
            if (!totp.VerifyTotp(dto.Code, out long timeStepMatched, new VerificationWindow(1, 1)))
            {
                return BadRequest(ApiResponse<object>.Error("Invalid or expired code"));
            }

            return Ok(ApiResponse<object>.Success("Code validated successfully"));
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Email does not exist
        /// - Invalid or expired code
        /// - Unable to reset password
        /// </remarks>
        /// <response code="200">Password reset successfully</response>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return BadRequest(ApiResponse<object>.Error("Email does not exist"));
            }

            // Derive a per-user secret from the JWT secret and user identifier
            var jwtSecret = _configuration["JWT:SigningKey"];
            var userSecret = $"{jwtSecret}-{user.UserId}";
            var totp = new Totp(Encoding.UTF8.GetBytes(userSecret), step: 3600); // 1 hour time step

            // Validate the submitted code
            if (!totp.VerifyTotp(dto.Code, out long timeStepMatched, new VerificationWindow(1, 1)))
            {
                return BadRequest(ApiResponse<object>.Error("Invalid or expired code"));
            }

            // Reset the user's password
            user.PasswordHash = dto.NewPassword;
            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to reset password"));
            }

            return Ok(ApiResponse<object>.Success("Password reset successfully"));
        }
    }
}