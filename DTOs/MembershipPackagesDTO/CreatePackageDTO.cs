﻿namespace SWD392.DTOs.MembershipPackagesDTO
{
    public class CreatePackageDTO
    {
        
        public string MembershipPackageName { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public int ValidityPeriod { get; set; }
        public decimal PercentDiscount { get; set; }
        public string Summary { get; set; }
        public List<int> Permissions { get; set; } = new List<int>();
    }
}
