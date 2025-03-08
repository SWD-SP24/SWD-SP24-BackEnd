namespace SWD392.DTOs.GrowthIndicatorDTO
{
    public class CreateGrowthIndicatorDTO
    {
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public required string RecordTime { get; set; } // Changed to string
        public int ChildrenId { get; set; }

    }
}
