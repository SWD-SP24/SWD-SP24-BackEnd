﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class Permission
{
    public int PermissionId { get; set; }

    public string PermissionName { get; set; }

    public string Description { get; set; }

    public virtual ICollection<MembershipPackage> MembershipPackages { get; set; } = new List<MembershipPackage>();
}