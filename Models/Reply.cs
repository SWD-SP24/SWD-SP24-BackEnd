﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class Reply
{
    public int ReplyId { get; set; }

    public string Content { get; set; }

    public int FeedbackId { get; set; }

    public int AdminId { get; set; }

    public virtual User Admin { get; set; }

    public virtual Feedback Feedback { get; set; }
}