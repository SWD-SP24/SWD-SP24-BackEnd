using SWD392.DTOs.ToothDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class ToothMapper
    {
        public static GetToothDTO ToGetToothDTO(this Tooth tooth)
        {
            return new GetToothDTO
            {
                Id = tooth.Id,
                NumberOfTeeth = tooth.NumberOfTeeth,
                TeethingPeriod = tooth.TeethingPeriod,
                Name = tooth.Name
            };
        }
    }

}
