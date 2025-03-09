using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Communication.Email;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using OtpNet;
using PayPal.Api;
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
        //private readonly FirebaseService _authentication;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly Cloudinary _cloudinary;

        public UsersController(AppDbContext context, IConfiguration configuration, Cloudinary cloudinary)
        {
            _context = context;
            //_authentication = new FirebaseService();
            _tokenService = new TokenService(configuration);
            var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
            _emailService = new EmailService(connectionString ?? "", configuration);
            _configuration = configuration;
            _cloudinary = cloudinary;

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
            string uid = "a";
            //try
            //{
            //    uid = await _authentication.RegisterAsync(userDTO.Email, userDTO.Password);
            //}
            //catch (Exception)
            //{
            //    return BadRequest(ApiResponse<object>.Error("Fail to create account (FB)"));
            //}

            var newUser = userDTO.ToUser(uid);

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                //await _authentication.DeleteAsync(uid);
                return BadRequest(ApiResponse<object>.Error("Fail to create account (DB)"));
            }
            //_context.Users.Add(newUser);
            //await _context.SaveChangesAsync();

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
                token = _tokenService.CreateUserToken(newUser);
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to create JWT Token"));
            }

            var response = await new AutoFreeMembershipPackage(_context).AutoPurchaseFreePackage(newUser.UserId.ToString());

            return Ok(ApiResponse<object>.Success(newUser.ToLoginResponseDTO(token)));
        }

        /// <summary>
        /// Resend verification email to the currently logged-in user (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// - Email is already verified
        /// </remarks>
        /// <response code="200">Verification email sent</response>
        [Authorize]
        [HttpPost("resend-verification-email")]
        public async Task<ActionResult> ResendVerificationEmail()
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            if (user.EmailActivation == "activated")
            {
                return BadRequest(ApiResponse<object>.Error("Email is already verified"));
            }

            _ = Task.Run(() =>
            {
                try
                {
                    var verifyToken = _tokenService.CreateVerifyEmailToken(user);
                    // Send account confirmation email
                    _emailService.SendAccountConfirmationEmail(user.Email, verifyToken);
                }
                catch (Exception)
                {
                    // TODO: Find a way to handle this error
                }
            });

            return Ok(ApiResponse<object>.Success("Verification email sent"));
        }


        /// <summary>
        /// Login user (All user)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Password is incorrect
        /// - Unable to create JWT Token
        /// - Member only
        /// </remarks>
        /// <response code="200">Logged in</response>

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> LoginUser(LoginUserDTO userDTO)
        {
            // TODO: support login with multiple methods
            var loginUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == userDTO.Email);
            if (loginUser == null) { return BadRequest(ApiResponse<object>.Error("Account does not exist")); }

            if (loginUser.PasswordHash != userDTO.Password) { return BadRequest(ApiResponse<object>.Error("Password is incorrect")); }

            string token = "";
            try
            {
                if (loginUser.Role == "admin")
                {
                    token = _tokenService.CreateAdminToken(loginUser);
                }
                else if (loginUser.Role == "doctor")
                {
                    token = _tokenService.CreateDoctorToken(loginUser);
                } 
                else
                {
                    token = _tokenService.CreateUserToken(loginUser);
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

        // GET: api/Users
        /// <summary>
        /// Get user list (admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// </remarks>
        /// <response code="200">Users retrieved</response>
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetUserDTO>>> GetUsers(int pageNumber = 1, int pageSize = 999)
        {
            var totalUsers = await _context.Users.CountAsync();
            var users = await _context.Users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDTOs = users.Select(user => user.ToGetUserDTO()).ToList();
            var hasNext = (pageNumber * pageSize) < totalUsers;
            var maxPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var pagination = new Pagination(maxPages, hasNext, totalUsers);

            return Ok(ApiResponse<object>.Success(userDTOs, pagination));
        }

        // GET: api/Users/doctors
        /// <summary>
        /// Get list of doctors
        /// </summary>
        /// <remarks>
        /// Errors:
        /// </remarks>
        /// <response code="200">Doctors retrieved</response>
        [HttpGet("doctors")]
        public async Task<ActionResult<IEnumerable<GetUserDTO>>> GetDoctors(int pageNumber = 1, int pageSize = 999)
        {
            var totalDoctors = await _context.Users.CountAsync(u => u.Role == "doctor");
            var doctors = await _context.Users
                .Where(u => u.Role == "doctor")
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var doctorDTOs = doctors.Select(doctor => doctor.ToGetUserDTO()).ToList();
            var hasNext = (pageNumber * pageSize) < totalDoctors;
            var maxPages = (int)Math.Ceiling(totalDoctors / (double)pageSize);

            var pagination = new Pagination(maxPages, hasNext, totalDoctors);

            return Ok(ApiResponse<object>.Success(doctorDTOs, pagination));
        }

        /// <summary>
        /// Get currently logged in user (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">User retrieved</response>
        [Authorize]
        [HttpGet("self")]
        public async Task<ActionResult<GetUserDTO>> GetSelf()
        {

            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key")); // or whatever

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            return Ok(ApiResponse<object>.Success(user.ToGetUserDTO()));
        }


        // GET: api/Users/5
        /// <summary>
        /// Get user with ID (admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// </remarks>
        /// <response code="200">User retrieved</response>
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GetUserDTO>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist"));
            }

            return Ok(ApiResponse<object>.Success(user.ToGetUserDTO()));
        }

        // GET: api/Users/5
        /// <summary>
        /// Edit currently logged in user (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">self edited</response>
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> EditSelf(EditUserDTO userDto)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key")); // or whatever

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            return await PutUser(user.UserId, userDto);
        }

        // PUT: api/Users/5
        /// <summary>
        /// Update user with specified ID (admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Unable to edit user
        /// </remarks>
        /// <response code="200">User edited</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, EditUserDTO userDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist"));
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
                if (userDto.Address != null)
                {
                    user.Address = userDto.Address;
                }
                if (userDto.Zipcode != null)
                {
                    user.Zipcode = userDto.Zipcode;
                }
                if (userDto.State != null)
                {
                    user.State = userDto.State;
                }
                if (userDto.Country != null)
                {
                    user.Country = userDto.Country;
                }
                if (userDto.Specialization != null)
                {
                    user.Specialization = userDto.Specialization;
                }
                if (userDto.LicenseNumber != null)
                {
                    user.LicenseNumber = userDto.LicenseNumber;
                }
                if (userDto.Hospital != null)
                {
                    user.Hospital = userDto.Hospital;
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
                    return NotFound(ApiResponse<object>.Error("Account does not exist"));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return BadRequest(ApiResponse<object>.Error("Unable to edit user"));
            }

            return Ok(ApiResponse<object>.Success(user.ToGetUserDTO()));
        }

        /// <summary>
        /// Delete user with specified ID <b>Broken do not use!!!!</b> (admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - Account does not exist
        /// - Unable to delete user (SQL)
        /// - Unable to delete user (Firebase)
        /// </remarks>
        /// <response code="200">Delete successful</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Error("Account does not exist"));
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

            //try
            //{
            //    await _authentication.DeleteAsync(user.Uid);
            //}
            //catch (Exception)
            //{
            //    return BadRequest(ApiResponse<object>.Error("Unable to delete user (Firebase)"));
            //}

            return Ok(ApiResponse<object>.Success("", message: "Delete successful"));
        }

        /// <summary>
        /// Check if user with id exists (admin only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// </remarks>
        /// <response code="200">successful</response>
        [Authorize(Roles = "admin")]
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
            User user;
            try
            {
                user = await ValidateJwtToken(token);
            }
            catch (UnauthorizedAccessException e)
            {
                return BadRequest(ApiResponse<object>.Error(e.Message));
            }

            user.EmailActivation = "activated";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Redirect("https://emailverified.hungngblog.com/");
        }

        // POST: api/Users/change-password
        /// <summary>
        /// Change user password (Authorized only)
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
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO changePasswordDTO)
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

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

        // GET: api/Users/children-count
        /// <summary>
        /// Get the total number of children for the currently logged-in user (Authorized only)
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No JWT key
        /// - JWT token has expired
        /// - Invalid JWT key
        /// </remarks>
        /// <response code="200">Children count retrieved</response>
        [Authorize]
        [HttpGet("children-count")]
        public async Task<ActionResult<object>> GetChildrenCount()
        {
            if (!HttpContext.Request.Headers.ContainsKey("Authorization"))
                return Unauthorized(ApiResponse<object>.Error("No JWT key"));

            var authHeader = HttpContext.Request.Headers["Authorization"][0];

            User user;
            try
            {
                user = await ValidateJwtToken(authHeader);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(ApiResponse<object>.Error(e.Message));
            }

            var childrenCount = await _context.Children.CountAsync(c => c.MemberId == user.UserId && c.Status != 0);

            return Ok(ApiResponse<object>.Success(new { childno = childrenCount }));
        }

        // POST: api/Users/UploadAvatar
        /// <summary>
        /// Upload an avatar for the currently logged-in user
        /// </summary>
        /// <remarks>
        /// Errors:
        /// - No file uploaded
        /// - Upload failed
        /// </remarks>
        /// <response code="200">Avatar uploaded successfully</response>
        /// <response code="400">No file uploaded</response>
        /// <response code="500">Upload failed</response>
        [Authorize]
        [HttpPost("UploadAvatar")]
        public async Task<ActionResult<ApiResponse<object>>> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Error("No file uploaded."));
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var authHeader = HttpContext.Request.Headers["Authorization"][0];
                var user = await ValidateJwtToken(authHeader);

                user.Avatar = uploadResult.Url.ToString();
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Success(new { uploadResult.Url }, message: "Avatar uploaded successfully"));
            }
            else
            {
                return StatusCode((int)uploadResult.StatusCode, ApiResponse<object>.Error(uploadResult.Error.Message));
            }
        }

        private async Task<User> ValidateJwtToken(string authHeader)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring("Bearer ".Length).Trim() : authHeader;

            var jwtToken = handler.ReadJwtToken(token);

            // Check if token has expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
                throw new UnauthorizedAccessException("JWT token has expired");

            var rawId = jwtToken.Claims.First(claim => claim.Type == "id").Value;
            var id = int.Parse(rawId);

            var user = await _context.Users.FindAsync(id) ?? throw new UnauthorizedAccessException("Invalid JWT key");
            return user;
        }
    }
}