namespace SWD392.DTOs.TeethingRecordsDTO
{
    public class EditTeethingRecordDTO
    {
        public int ToothNumber { get; set; } // Change to ToothNumber
        public DateTime? EruptionDate { get; set; }
        public DateTime? RecordTime { get; set; }
        public string? Note { get; set; } // Add this line

    }
}
