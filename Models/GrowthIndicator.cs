﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class GrowthIndicator
{
    public int GrowthIndicatorsId { get; set; }

    public int Height { get; set; }

    public int Weight { get; set; }

    public int Bmi { get; set; }

    public DateTime RecordTime { get; set; }

    public int ChildrenId { get; set; }

    public virtual Child Children { get; set; }
}