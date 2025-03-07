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
                ChildId = teethingRecord.ChildId,
                ToothId = teethingRecord.ToothId,
                EruptionDate = teethingRecord.EruptionDate,
                RecordTime = teethingRecord.RecordTime,
                ChildName = teethingRecord.Child!.FullName, // Using null-forgiving operator
                ToothName = teethingRecord.Tooth!.Name // Using null-forgiving operator
            };
        }
    }
}
