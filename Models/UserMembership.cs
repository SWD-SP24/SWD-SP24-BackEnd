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

    public int? Paymenttransactionid { get; set; }

    public virtual MembershipPackage MembershipPackage { get; set; }

    public virtual Paymenttransaction Paymenttransaction { get; set; }

    public virtual ICollection<Paymenttransaction> Paymenttransactions { get; set; } = new List<Paymenttransaction>();

    public virtual User User { get; set; }
}