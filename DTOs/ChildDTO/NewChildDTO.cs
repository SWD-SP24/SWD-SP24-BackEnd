namespace SWD392.DTOs.ChildDTO
{
    public class NewChildDTO
    {
        public required string FullName { get; set; }
        public required int Age { get; set; }
        public string? Avatar { get; set; }
        public required int MemberId { get; set; }

    }
}
