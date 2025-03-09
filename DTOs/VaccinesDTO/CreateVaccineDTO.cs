namespace SWD392.DTOs.VaccinesDTO
{
    public class CreateVaccineDTO
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int? DosesRequired { get; set; }
    }
}
