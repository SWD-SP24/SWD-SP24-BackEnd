using SWD392.DTOs.VaccinesDTO;
using SWD392.Models;

namespace SWD392.Mapper
{
    public static class VaccineMapper
    {
        public static VaccineDTO ToVaccineDto(this Vaccine vaccine)
        {
            return new VaccineDTO
            {
                Id = vaccine.Id,
                Name = vaccine.Name,
                Description = vaccine.Description,
                DosesRequired = vaccine.DosesRequired
            };
        }

        public static Vaccine ToVaccine(this CreateVaccineDTO createVaccineDto)
        {
            return new Vaccine
            {
                Name = createVaccineDto.Name,
                Description = createVaccineDto.Description,
                DosesRequired = createVaccineDto.DosesRequired
            };
        }

        public static void UpdateVaccine(this EditVaccineDTO editVaccineDto, Vaccine vaccine)
        {
            vaccine.Name = editVaccineDto.Name;
            vaccine.Description = editVaccineDto.Description;
            vaccine.DosesRequired = editVaccineDto.DosesRequired;
        }
    }
}
