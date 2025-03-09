namespace SWD392.DTOs.VaccinationScheduleDTOs
{
    public class VaccinationScheduleDTO
    {
        public int Id { get; set; }
        public int? VaccineId { get; set; }
        public int? RecommendedAgeMonths { get; set; }
        public string? VaccineName { get; set; }
    }

    public class CreateVaccinationScheduleDTO
    {
        public int? VaccineId { get; set; }
        public int? RecommendedAgeMonths { get; set; }
    }

    public class EditVaccinationScheduleDTO
    {
        public int? VaccineId { get; set; }
        public int? RecommendedAgeMonths { get; set; }
    }
}
