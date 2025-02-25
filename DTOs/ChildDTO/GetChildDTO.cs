namespace SWD392.DTOs.ChildDTO
{
    public class GetChildDTO
    {
        public int ChildrenId { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Avatar { get; set; }
        public int MemberId { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
