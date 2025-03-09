using SWD392.DTOs.VaccinationScheduleDTOs;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class VaccinationScheduleMapper
    {
        public static VaccinationScheduleDTO ToVaccinationScheduleDto(this VaccinationSchedule vaccinationSchedule)
        {
            return new VaccinationScheduleDTO
            {
                Id = vaccinationSchedule.Id,
                VaccineId = vaccinationSchedule.VaccineId,
                RecommendedAgeMonths = vaccinationSchedule.RecommendedAgeMonths,
                VaccineName = vaccinationSchedule.Vaccine?.Name,
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
