namespace SWD392.DTOs.TeethingRecordsDTO
{
    public class CreateTeethingRecordDTO
    {
        public int ChildId { get; set; }
        public int ToothNumber { get; set; } // Change to ToothNumber
        public DateTime? EruptionDate { get; set; }
        public DateTime? RecordTime { get; set; }
        public string? Note { get; set; }

    }
}
