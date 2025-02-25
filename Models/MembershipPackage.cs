﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class MembershipPackage
{
    public int MembershipPackageId { get; set; }

    public string MembershipPackageName { get; set; }

    public decimal Price { get; set; }

    public int ValidityPeriod { get; set; }

    public string Status { get; set; }

    public DateTime CreatedTime { get; set; }

    public int? AdminId { get; set; }
    public string image { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<UserMembership> UserMemberships { get; set; } = new List<UserMembership>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}