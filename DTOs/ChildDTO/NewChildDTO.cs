namespace SWD392.DTOs.ChildDTO
{
    public class NewChildDTO
    {
        public required string FullName { get; set; }
        public required int Age { get; set; }
        public string? Avatar { get; set; }
        public required int MemberId { get; set; }
        public required DateOnly Dob { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicConditions { get; set; }
        public required string Gender { get; set; }

    }
}
