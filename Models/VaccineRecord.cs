﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class VaccineRecord
{
    public int Id { get; set; }

    public int ChildId { get; set; }

    public int VaccineId { get; set; }

    public DateTime AdministeredDate { get; set; }

    public int? Dose { get; set; }

    public DateTime? NextDoseDate { get; set; }

    public virtual Child Child { get; set; }

    public virtual Vaccine Vaccine { get; set; }
}