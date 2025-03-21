﻿using Microsoft.IdentityModel.Tokens;
using SWD392.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SWD392.Service
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly SymmetricSecurityKey _key;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SigningKey"]));
        }

        public string CreateUserToken(User user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.UniqueName, user.Email),
                new(ClaimTypes.Role, "member"),
                new("id", user.UserId.ToString()),
                new("uid", user.Uid),
            };

            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = credentials,
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return $"Bearer {tokenHandler.WriteToken(token)}";
        }

        public string CreateAdminToken(User user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.UniqueName, user.Email),
                new(ClaimTypes.Role, "admin"),
                new("id", user.UserId.ToString()),
                new("uid", user.Uid),
            };

            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = credentials,
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return $"Bearer {tokenHandler.WriteToken(token)}";
        }

        public string CreateDoctorToken(User user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.UniqueName, user.Email),
                new(ClaimTypes.Role, "doctor"),
                new("id", user.UserId.ToString()),
                new("uid", user.Uid),
            };

            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = credentials,
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return $"Bearer {tokenHandler.WriteToken(token)}";
        }

        public string CreateVerifyEmailToken(User user)
        {
            var claims = new List<Claim>
            {
                new("id", user.UserId.ToString()),
                new("uid", user.Uid),
            };

            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentials,
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return $"{tokenHandler.WriteToken(token)}";
        }

    }
}
