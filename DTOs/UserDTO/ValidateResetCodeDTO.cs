namespace SWD392.DTOs.UserDTO
{
    public class ValidateResetCodeDTO
    {
        public required string Email { get; set; }
        public required string Code { get; set; }
    }
}
