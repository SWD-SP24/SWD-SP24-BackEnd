namespace SWD392.DTOs.VaccineRecordDTO
{
    public class VaccineRecordDto
    {
        public int Id { get; set; }
        public int ChildId { get; set; }
        public int VaccineId { get; set; }
        public required string AdministeredDate { get; set; }
        public int? Dose { get; set; }
        public string? NextDoseDate { get; set; }
        public required string ChildName { get; set; }
        public required string VaccineName { get; set; }
    }

    public class UpdateVaccineRecordDto
    {
        public string? AdministeredDate { get; set; }
    }

    public class CreateVaccineRecordDto
    {
        public required int ChildId { get; set; }
        public required int VaccineId { get; set; }
        public required string AdministeredDate { get; set; }
        public int? Dose { get; set; }
    }
}
