﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class Child
{
    public int ChildrenId { get; set; }

    public string FullName { get; set; }

    public string Avatar { get; set; }

    public int MemberId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateOnly? Dob { get; set; }

    public string BloodType { get; set; }

    public string Allergies { get; set; }

    public string ChronicConditions { get; set; }

    public string Gender { get; set; }

    public virtual ICollection<ConsultationNote> ConsultationNotes { get; set; } = new List<ConsultationNote>();

    public virtual ICollection<GrowthIndicator> GrowthIndicators { get; set; } = new List<GrowthIndicator>();

    public virtual User Member { get; set; }

    public virtual ICollection<TeethingRecord> TeethingRecords { get; set; } = new List<TeethingRecord>();

    public virtual ICollection<VaccinationSchedule> VaccinationSchedules { get; set; } = new List<VaccinationSchedule>();

    public virtual ICollection<VaccineRecord> VaccineRecords { get; set; } = new List<VaccineRecord>();
}