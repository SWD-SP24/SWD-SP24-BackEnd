namespace SWD392.DTOs.ToothDTO
{
    public class CreateToothDTO
    {
        public int NumberOfTeeth { get; set; }
        public required int TeethingPeriod { get; set; }
        public required string Name { get; set; }
    }
}
