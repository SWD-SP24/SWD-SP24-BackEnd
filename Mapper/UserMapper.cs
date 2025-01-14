using SWD392.DTOs.UserDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class UserMapper
    {
        public static User ToUser(this RegisterUserDTO registerUserDTO, string uid)
        {
            return new User
            {
                Email = registerUserDTO.Email,
                PhoneNumber = registerUserDTO.PhoneNumber ?? null,
                PasswordHash = registerUserDTO.Password,
                FullName = registerUserDTO.FullName ?? registerUserDTO.Email,
                Avatar = "",
                Role = "member",
                Status = "active",
                CreatedAt = DateTime.Now,
                MembershipPackageId = null, // TODO: fix when MembershipPackage is implemented
                Uid = uid
            };
        }

        public static User ToUser(this LoginUserDTO loginUserDTO)
        {
            return new User
            {
                Email = loginUserDTO.Email,
                PasswordHash = loginUserDTO.Password
            };
        }

        public static GetUserDTO ToGetUserDTO(this User user)
        {
            return new GetUserDTO
            {
                UserId = user.UserId,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PasswordHash = user.PasswordHash,
                FullName = user.FullName,
                Avatar = user.Avatar,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                MembershipPackageId = user.MembershipPackageId,
                Uid = user.Uid,
            };
        }

        public static LoginResponseDTO ToLoginResponseDTO(this User user, string token)
        {
            return new LoginResponseDTO
            {
                User = user.ToGetUserDTO(),
                Token = token
            };
        }
    }
}
