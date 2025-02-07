using SWD392.Models;

namespace SWD392.DTOs.UserDTO
{
    public class RegisterUserDTO
    {
        public required string Email { get; set; }

        public required string Password { get; set; }

        //public int? MembershipPackageId { get; set; }
    }
}
