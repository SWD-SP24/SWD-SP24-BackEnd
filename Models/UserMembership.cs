﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class UserMembership
{
    public int UserMembershipId { get; set; }

    public int UserId { get; set; }

    public int MembershipPackageId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Status { get; set; }

    public int? PaymentTransactionId { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public decimal YearlyPriceAtPurchase { get; set; }

    public virtual MembershipPackage MembershipPackage { get; set; }

    public virtual PaymentTransaction PaymentTransaction { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual User User { get; set; }

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}