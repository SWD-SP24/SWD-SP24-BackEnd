using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD392.Data;
using SWD392.DTOs.UserDTO;
using SWD392.Mapper;
using SWD392.Service;

namespace SWD392.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FirebaseService _authentication;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AdminAuthController(AppDbContext context, IConfiguration configuration)
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
        /// Register admin
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Email already exists.
        /// - Fail to create account (FB)  Check for valid email.
        /// - Fail to create account (DB)
        /// </remarks>
        /// <response code="200">User register</response>
        [HttpPost("registerAdmin")]
        public async Task<ActionResult<GetUserDTO>> RegisterAdmin(RegisterUserDTO userDTO)
        {
            if (_context.Users.Any(_context => _context.Email == userDTO.Email))
            {
                //return BadRequest(new {status = 1000, message = "Email already exists."}); 
                return BadRequest(ApiResponse<object>.Error("Email already exists."));
            }
            string uid = "a";
            //try
            //{
            //    uid = await _authentication.RegisterAsync(userDTO.Email, userDTO.Password);
            //}
            //catch (Exception)
            //{
            //    return BadRequest(ApiResponse<object>.Error("Fail to create account (FB)"));
            //}

            var newUser = userDTO.ToAdmin(uid);

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

            _ = Task.Run(() =>
            {
                try
                {
                    var verifyToken = _tokenService.CreateVerifyEmailToken(newUser);
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
                token = _tokenService.CreateAdminToken(newUser);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to create JWT Token"));
            }

            var response = await new AutoFreeMembershipPackage(_context).AutoPurchaseFreePackage(newUser.UserId.ToString());

            return Ok(ApiResponse<object>.Success(newUser.ToLoginResponseDTO(token)));
        }

        // POST: api/Users/
        /// <summary>
        /// Register doctor (admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Email already exists.
        /// - Fail to create account (FB)  Check for valid email.
        /// - Fail to create account (DB)
        /// </remarks>
        /// <response code="200">User register</response>
        [Authorize(Roles = "admin")]
        [HttpPost("registerDoctor")]
        public async Task<ActionResult<GetUserDTO>> RegisterDoctor(RegisterUserDTO userDTO)
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
            string uid = "a";
            //try
            //{
            //    uid = await _authentication.RegisterAsync(userDTO.Email, userDTO.Password);
            //}
            //catch (Exception)
            //{
            //    return BadRequest(ApiResponse<object>.Error("Fail to create account (FB)"));
            //}

            var newUser = userDTO.ToDoctor(uid);

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

            _ = Task.Run(() =>
            {
                try
                {
                    var verifyToken = _tokenService.CreateVerifyEmailToken(newUser);
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
                token = _tokenService.CreateDoctorToken(newUser);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to create JWT Token"));
            }

            var response = await new AutoFreeMembershipPackage(_context).AutoPurchaseFreePackage(newUser.UserId.ToString());

            return Ok(ApiResponse<object>.Success(newUser.ToLoginResponseDTO(token)));
        }

        /// <summary>
        /// Login system
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Password is incorrect
        /// - Unable to create JWT Token
        /// - Doctor or admin only
        /// </remarks>
        /// <response code="200">Logged in</response>
        [HttpPost("loginSystem")]
        private async Task<ActionResult<LoginResponseDTO>> LoginSystem(LoginUserDTO userDTO)
        {
            // TODO: support login with multiple methods
            // TODO: Confirm email
            // TODO: Reset password

            var loginUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == userDTO.Email);
            if (loginUser == null) { return BadRequest(ApiResponse<object>.Error("Account does not exist")); }

            if (loginUser.Role != "admin" && loginUser.Role != "doctor") { return Unauthorized(ApiResponse<object>.Error("Doctor or admin only")); }
            if (loginUser.PasswordHash != userDTO.Password) { return BadRequest(ApiResponse<object>.Error("Password is incorrect")); }

            string token = "";
            try
            {
                if (loginUser.Role == "admin")
                {
                    token = _tokenService.CreateAdminToken(loginUser);
                }
                else
                {
                    token = _tokenService.CreateDoctorToken(loginUser);
                }
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to create JWT Token"));
            }

            var loginResponse = loginUser.ToLoginResponseDTO(token);

            var response = await new AutoFreeMembershipPackage(_context).AutoPurchaseFreePackage(loginUser.UserId.ToString());

            return Ok(ApiResponse<object>.Success(loginResponse));
        }
    }
}
