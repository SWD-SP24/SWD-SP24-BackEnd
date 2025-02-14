﻿using Microsoft.AspNetCore.Http;
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
        /// Register user
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
    }
}
