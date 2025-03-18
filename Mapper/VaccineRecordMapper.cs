using SWD392.DTOs.VaccineRecordDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class VaccineRecordMapper
    {
        public static VaccineRecordDto ToDto(VaccineRecord vaccineRecord)
        {
            return new VaccineRecordDto
            {
                Id = vaccineRecord.Id,
                ChildId = vaccineRecord.ChildId,
                VaccineId = vaccineRecord.VaccineId,
                AdministeredDate = vaccineRecord.AdministeredDate.ToString("dd/MM/yyyy"), // Format as needed
                Dose = vaccineRecord.Dose,
                NextDoseDate = vaccineRecord.NextDoseDate?.ToString("dd/MM/yyyy"), // Format as needed
                ChildName = vaccineRecord.Child.FullName, // Assuming Child has a FullName property
                VaccineName = vaccineRecord.Vaccine.Name // Assuming Vaccine has a Name property
            };
        }
    }
}
