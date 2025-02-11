namespace SWD392.DTOs.UserDTO
{
    public class ResetPasswordDTO
    {
        public required string Email { get; set; }
        public required string Code { get; set; }
        public required string NewPassword { get; set; }
    }
}
