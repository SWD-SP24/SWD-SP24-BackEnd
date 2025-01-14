using SWD392.Models;

namespace SWD392.DTOs.UserDTO
{
    public class LoginResponseDTO
    {
        public required User User { get; set; }
        public required string Token { get; set; }
    }
}
