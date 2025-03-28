﻿using SWD392.DTOs.MembershipPackagesDTO;
using SWD392.DTOs.PaymentTransactionDTO;
using SWD392.Models;

namespace SWD392.DTOs.UserMembershipDTO
{
    public class GetCurrentPackageDTO
    {
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Status { get; set; }
        public virtual GetMembershipPackageDTO MembershipPackage { get; set; }

    }
}
