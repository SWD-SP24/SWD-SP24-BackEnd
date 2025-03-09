namespace SWD392.DTOs.MembershipPackagesDTO
{
    public class OrderDetail2DTO
    {
        public int MembershipPackageId { get; set; }

        public string MembershipPackageName { get; set; }

        public decimal Price { get; set; }
        public string Status { get; set; }
        public string Image { get; set; }
        public string Summary { get; set; }
        public int ValidityPeriod { get; set; }
        public decimal SavingPerMonth { get; set; }
        public decimal YearlyPrice  { get; set; }
        public int PercentDiscount { get; set; }

        public virtual ICollection<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }
}
