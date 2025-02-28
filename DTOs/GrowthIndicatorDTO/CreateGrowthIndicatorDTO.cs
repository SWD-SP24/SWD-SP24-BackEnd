namespace SWD392.DTOs.GrowthIndicatorDTO
{
    public class CreateGrowthIndicatorDTO
    {
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public DateTime RecordTime { get; set; }
        public int ChildrenId { get; set; }

    }
}
