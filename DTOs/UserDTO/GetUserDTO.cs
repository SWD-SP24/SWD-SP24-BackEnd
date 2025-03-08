using SWD392.Models;

namespace SWD392.DTOs.UserDTO
{
    public class GetUserDTO
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

        public string? EmailActivation { get; set; }
        public string? Address { get; set; }
        public string? Zipcode { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Hospital { get; set; }
    }
}
