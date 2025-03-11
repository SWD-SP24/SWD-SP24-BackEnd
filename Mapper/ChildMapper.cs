using SWD392.DTOs.ChildDTO;
using SWD392.Models;
using System.Globalization;

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
                CreatedAt = child.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                Dob = child.Dob,
                BloodType = child.BloodType,
                Allergies = child.Allergies,
                ChronicConditions = child.ChronicConditions,
                Gender = child.Gender,
                Status = child.Status
            };
        }
    }

}
