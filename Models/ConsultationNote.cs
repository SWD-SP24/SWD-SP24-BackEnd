﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class ConsultationNote
{
    public int ConsultationNoteId { get; set; }

    public string Content { get; set; }

    public DateTime RecordTime { get; set; }

    public int MemberId { get; set; }

    public int DoctorId { get; set; }

    public int ChildrenId { get; set; }

    public virtual Child Children { get; set; }

    public virtual User Doctor { get; set; }

    public virtual User Member { get; set; }
}