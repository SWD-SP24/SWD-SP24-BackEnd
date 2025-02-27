namespace SWD392.DTOs.ChildDTO
{
    public class GetChildDTO
    {
        public int ChildrenId { get; set; }
        public required string FullName { get; set; }
        public string? Avatar { get; set; }
        public int MemberId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateOnly? Dob { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicConditions { get; set; }
        public required string Gender { get; set; }

    }
}
