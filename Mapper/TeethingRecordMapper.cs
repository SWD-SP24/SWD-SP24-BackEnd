using SWD392.DTOs.TeethingRecordsDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class TeethingRecordMapper
    {
        public static TeethingRecordDTO ToTeethingRecordDto(this TeethingRecord teethingRecord)
        {
            return new TeethingRecordDTO
            {
                Id = teethingRecord.Id,
                RecordTime = teethingRecord.RecordTime,
                Note = teethingRecord.Note,
                ChildId = teethingRecord.Child?.ChildrenId ?? 0,
                ChildName = teethingRecord.Child?.FullName ?? string.Empty,
                ToothId = teethingRecord.Tooth?.Id ?? 0,
                ToothName = teethingRecord.Tooth?.Name ?? string.Empty
            };
        }
    }
}
