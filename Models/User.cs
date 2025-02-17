﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace SWD392.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Uid { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string PasswordHash { get; set; }

    public string FullName { get; set; }

    public string Avatar { get; set; }

    public string Role { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? MembershipPackageId { get; set; }

    public string EmailActivation { get; set; }

    public virtual ICollection<BlogContent> BlogContents { get; set; } = new List<BlogContent>();

    public virtual ICollection<Child> Children { get; set; } = new List<Child>();

    public virtual ICollection<ConsultationNote> ConsultationNoteDoctors { get; set; } = new List<ConsultationNote>();

    public virtual ICollection<ConsultationNote> ConsultationNoteMembers { get; set; } = new List<ConsultationNote>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual MembershipPackage MembershipPackage { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<Reply> Replies { get; set; } = new List<Reply>();

    public virtual ICollection<UserMembership> UserMemberships { get; set; } = new List<UserMembership>();
}