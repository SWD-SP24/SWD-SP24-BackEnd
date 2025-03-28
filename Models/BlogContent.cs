﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class BlogContent
{
    public int BlogContentId { get; set; }

    public string Title { get; set; }

    public string Content { get; set; }

    public string ThumbnailUrl { get; set; }

    public string Status { get; set; }

    public int Views { get; set; }

    public int Likes { get; set; }

    public DateTime CreatedAt { get; set; }

    public int AdminId { get; set; }

    public virtual User Admin { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}