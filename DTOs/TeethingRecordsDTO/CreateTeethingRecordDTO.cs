namespace SWD392.DTOs.TeethingRecordsDTO
{
    public class CreateTeethingRecordDTO
    {
        public int ChildId { get; set; }
        public int ToothId { get; set; } // Change to ToothNumber
        public required string EruptionDate { get; set; }
        public string? RecordTime { get; set; }
        public string? Note { get; set; }

    }
}
