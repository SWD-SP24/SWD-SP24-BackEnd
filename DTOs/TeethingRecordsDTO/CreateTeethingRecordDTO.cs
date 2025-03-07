namespace SWD392.DTOs.TeethingRecordsDTO
{
    public class CreateTeethingRecordDTO
    {
        public int ChildId { get; set; }
        public int ToothId { get; set; }
        public DateTime? EruptionDate { get; set; }
        public DateTime? RecordTime { get; set; }
        public string Note { get; set; } // Add this line

    }
}
