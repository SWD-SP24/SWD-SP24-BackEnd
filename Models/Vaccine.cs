﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class Vaccine
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public int? DosesRequired { get; set; }

    public virtual ICollection<VaccinationSchedule> VaccinationSchedules { get; set; } = new List<VaccinationSchedule>();
}