namespace SWD392.DTOs.TeethingRecordsDTO
{
    public class TeethingRecordDTO
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public int ToothId { get; set; }
        public string? EruptionDate { get; set; }
        public string? RecordTime { get; set; }
        public required string ChildName { get; set; }
        public required string ToothName { get; set; }
        public string? Note { get; set; } // Add this line

    }
}
