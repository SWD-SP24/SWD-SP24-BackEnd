namespace SWD392.DTOs.VaccinesDTO
{
    public class VaccineDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int? DosesRequired { get; set; }
    }
}
