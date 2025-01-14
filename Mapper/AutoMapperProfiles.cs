using AutoMapper;
using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<MembershipPackage, GetMembershipPackageDTO>()
               .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions)); // Ánh xạ Permissions

            // Ánh xạ từ Permission sang PermissionDTO
            CreateMap<Permission, PermissionDTO>(); // Ánh xạ trực tiếp từ Permission sang PermissionDTO
        }
    }
    }


