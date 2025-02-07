using SWD392.Models;

namespace SWD392.DTOs.UserDTO
{
    public class LoginResponseDTO
    {
        public int UserId { get; set; }

        public required string Email { get; set; }

        public required string PhoneNumber { get; set; }

        //public required string PasswordHash { get; set; }

        public required string FullName { get; set; }

        public required string Avatar { get; set; }

        public required string Role { get; set; }

        public required string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? MembershipPackageId { get; set; }

        public required string Uid { get; set; }

        public required string Token { get; set; }

        public string? EmailActivation { get; set; }
    }
}
