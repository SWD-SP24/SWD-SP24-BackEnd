﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class TeethingRecord
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public int ToothId { get; set; }

    public DateTime? EruptionDate { get; set; }

    public DateTime? RecordTime { get; set; }

    public string Note { get; set; }

    public virtual Child Child { get; set; }

    public virtual Tooth Tooth { get; set; }
}