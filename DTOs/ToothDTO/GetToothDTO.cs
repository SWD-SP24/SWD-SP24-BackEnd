using SWD392.Models;

namespace SWD392.DTOs.ToothDTO
{
    public class GetToothDTO
    {
        public int Id { get; set; }

        public int NumberOfTeeth { get; set; }

        public required string TeethingPeriod { get; set; }

        public required string Name { get; set; }

    }
}
