using SWD392.DTOs.GrowthIndicatorDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class GrowthIndicatorMapper
    {
        public static GrowthIndicatorDTO ToGrowthIndicatorDto(this GrowthIndicator growthIndicator)
        {
            return new GrowthIndicatorDTO
            {
                GrowthIndicatorsId = growthIndicator.GrowthIndicatorsId,
                Height = growthIndicator.Height,
                Weight = growthIndicator.Weight,
                Bmi = growthIndicator.Bmi,
                ChildrenId = growthIndicator.ChildrenId
            };
        }
    }
}
