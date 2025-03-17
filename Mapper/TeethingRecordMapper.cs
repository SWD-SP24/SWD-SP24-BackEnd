using SWD392.DTOs.TeethingRecordsDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class TeethingRecordMapper
    {
        public static TeethingRecordDTO ToTeethingRecordDto(this Teethingrecord teethingRecord)
        {
            return new TeethingRecordDTO
            {
                Id = teethingRecord.Id,
                ChildId = teethingRecord.Child?.ChildrenId ?? 0,
                ToothId = teethingRecord.Tooth?.Id ?? 0,
                EruptionDate = teethingRecord.EruptionDate?.ToString("dd/MM/yyyy"),
                RecordTime = teethingRecord.RecordTime?.ToString("dd/MM/yyyy"),
                ChildName = teethingRecord.Child?.FullName ?? string.Empty,
                ToothName = teethingRecord.Tooth?.Name ?? string.Empty,
                Note = teethingRecord.Note
            };
        }
    }
}
