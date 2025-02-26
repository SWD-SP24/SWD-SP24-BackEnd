using SWD392.DTOs.ChildDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class ChildMapper
    {
        public static GetChildDTO ToGetChildDTO(this Child child)
        {
            return new GetChildDTO
            {
                ChildrenId = child.ChildrenId,
                FullName = child.FullName,
                Avatar = child.Avatar,
                MemberId = child.MemberId,
                CreatedAt = child.CreatedAt,
                Dob = child.Dob,
                BloodType = child.BloodType,
                Allergies = child.Allergies,
                ChronicConditions = child.ChronicConditions,
                Gender = child.Gender
            };
        }
    }

}
