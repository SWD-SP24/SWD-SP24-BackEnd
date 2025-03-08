namespace SWD392.DTOs.GrowthIndicatorDTO
{
    public class GrowthIndicatorDTO
    {
        public int GrowthIndicatorsId { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal Bmi { get; set; }
        public int ChildrenId { get; set; }
        public required string RecordTime { get; set; }

    }
}
