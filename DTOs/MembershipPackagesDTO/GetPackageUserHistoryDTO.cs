namespace SWD392.DTOs.MembershipPackagesDTO
{
    public class GetPackageUserHistoryDTO
    {
        public int MembershipPackageId { get; set; }

        public string MembershipPackageName { get; set; }

        public decimal Price { get; set; }
        public string Status { get; set; }

        public decimal YearlyPrice { get; set; }
        public int? PercentDiscount { get; set; }
        public int ValidityPeriod { get; set; }
        public List<PermissionDTO> Permissions { get; set; }
    }
}
