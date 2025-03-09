using SWD392.Data;
using SWD392.DTOs.VaccinationScheduleDTOs;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class VaccinationScheduleMapper
    {
        public static VaccinationScheduleDTO ToVaccinationScheduleDto(this VaccinationSchedule vaccinationSchedule, AppDbContext context)
        {
            // Get all vaccination schedules for the same vaccine
            var schedules = context.VaccinationSchedules
                .Where(vs => vs.VaccineId == vaccinationSchedule.VaccineId)
                .OrderBy(vs => vs.RecommendedAgeMonths)
                .ToList();

            // Calculate the dose number based on the position in the list
            var doseNumber = schedules.FindIndex(vs => vs.Id == vaccinationSchedule.Id) + 1;

            return new VaccinationScheduleDTO
            {
                Id = vaccinationSchedule.Id,
                VaccineId = vaccinationSchedule.VaccineId,
                RecommendedAgeMonths = vaccinationSchedule.RecommendedAgeMonths,
                VaccineName = vaccinationSchedule.Vaccine?.Name,
                DoseNumber = doseNumber
            };
        }

        public static VaccinationSchedule ToVaccinationSchedule(this CreateVaccinationScheduleDTO createVaccinationScheduleDto)
        {
            return new VaccinationSchedule
            {
                VaccineId = createVaccinationScheduleDto.VaccineId,
                RecommendedAgeMonths = createVaccinationScheduleDto.RecommendedAgeMonths,
            };
        }
    }
}
