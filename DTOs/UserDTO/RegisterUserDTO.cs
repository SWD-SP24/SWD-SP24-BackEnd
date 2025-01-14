using SWD392.Models;

namespace SWD392.DTOs.UserDTO
{
    public class RegisterUserDTO
    {
        public required string Email { get; set; }

        public string? PhoneNumber { get; set; }

        public required string Password { get; set; }

        public string? FullName { get; set; }

        //public int? MembershipPackageId { get; set; }
    }
}
