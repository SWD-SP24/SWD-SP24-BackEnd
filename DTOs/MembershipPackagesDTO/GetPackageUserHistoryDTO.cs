namespace SWD392.DTOs.MembershipPackagesDTO
{
    public class GetPackageUserHistoryDTO
    {
        public int MembershipPackageId { get; set; }

        public string MembershipPackageName { get; set; }

        public decimal Price { get; set; }
        public string Status { get; set; }

        public decimal YearlyPrice { get; set; }
        public decimal PercentDiscount { get; set; }
        public int ValidityPeriod { get; set; }
        public virtual ICollection<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }
}
