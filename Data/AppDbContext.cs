﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SWD392.Models;

namespace SWD392.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BlogContent> BlogContents { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Child> Children { get; set; }

    public virtual DbSet<ConsultationNote> ConsultationNotes { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<GrowthIndicator> GrowthIndicators { get; set; }

    public virtual DbSet<MembershipPackage> MembershipPackages { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Reply> Replies { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserMembership> UserMemberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogContent>(entity =>
        {
            entity.HasKey(e => e.BlogContentId).HasName("PK__blog_con__48384C3472399107");

            entity.ToTable("blog_contents");

            entity.Property(e => e.BlogContentId).HasColumnName("blog_content_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Likes).HasColumnName("likes");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.ThumbnailUrl)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("thumbnail_url");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Views).HasColumnName("views");

            entity.HasOne(d => d.Admin).WithMany(p => p.BlogContents)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_blog_contents_admin");

            entity.HasMany(d => d.Categories).WithMany(p => p.BlogContents)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_blog_categories_categories"),
                    l => l.HasOne<BlogContent>().WithMany()
                        .HasForeignKey("BlogContentId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_blog_categories_blog_contents"),
                    j =>
                    {
                        j.HasKey("BlogContentId", "CategoryId").HasName("PK__blog_cat__156CA2AF92567902");
                        j.ToTable("blog_categories");
                        j.IndexerProperty<int>("BlogContentId").HasColumnName("blog_content_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__categori__D54EE9B4D94B836B");

            entity.ToTable("categories");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("category_name");
        });

        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.ChildrenId).HasName("PK__children__1DAECF183C9C3B57");

            entity.ToTable("children");

            entity.Property(e => e.ChildrenId).HasColumnName("children_id");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.MemberId).HasColumnName("member_id");

            entity.HasOne(d => d.Member).WithMany(p => p.Children)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_children_users");
        });

        modelBuilder.Entity<ConsultationNote>(entity =>
        {
            entity.HasKey(e => e.ConsultationNoteId).HasName("PK__consulta__C46F4A4066E89FAC");

            entity.ToTable("consultation_notes");

            entity.Property(e => e.ConsultationNoteId).HasColumnName("consultation_note_id");
            entity.Property(e => e.ChildrenId).HasColumnName("children_id");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.RecordTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("record_time");

            entity.HasOne(d => d.Children).WithMany(p => p.ConsultationNotes)
                .HasForeignKey(d => d.ChildrenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_consultation_notes_children");

            entity.HasOne(d => d.Doctor).WithMany(p => p.ConsultationNoteDoctors)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_consultation_notes_doctor");

            entity.HasOne(d => d.Member).WithMany(p => p.ConsultationNoteMembers)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_consultation_notes_member");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__feedback__7A6B2B8C2B660965");

            entity.ToTable("feedbacks");

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Rating).HasColumnName("rating");

            entity.HasOne(d => d.Member).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_feedbacks_users");
        });

        modelBuilder.Entity<GrowthIndicator>(entity =>
        {
            entity.HasKey(e => e.GrowthIndicatorsId).HasName("PK__growth_i__C307B743AD2D478D");

            entity.ToTable("growth_indicators");

            entity.Property(e => e.GrowthIndicatorsId).HasColumnName("growth_indicators_id");
            entity.Property(e => e.Bmi).HasColumnName("bmi");
            entity.Property(e => e.ChildrenId).HasColumnName("children_id");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.RecordTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("record_time");
            entity.Property(e => e.Weight).HasColumnName("weight");

            entity.HasOne(d => d.Children).WithMany(p => p.GrowthIndicators)
                .HasForeignKey(d => d.ChildrenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_growth_indicators_children");
        });

        modelBuilder.Entity<MembershipPackage>(entity =>
        {
            entity.HasKey(e => e.MembershipPackageId).HasName("PK__membersh__3BA5AAD9A8E46109");

            entity.ToTable("membership_packages");

            entity.HasIndex(e => e.MembershipPackageName, "UQ__membersh__53772C879A4BC1B6").IsUnique();

            entity.Property(e => e.MembershipPackageId).HasColumnName("membership_package_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_time");
            entity.Property(e => e.MembershipPackageName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("membership_package_name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.ValidityPeriod).HasColumnName("validity_period");

            entity.HasMany(d => d.Permissions).WithMany(p => p.MembershipPackages)
                .UsingEntity<Dictionary<string, object>>(
                    "PackagePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_package_permissions_permissions"),
                    l => l.HasOne<MembershipPackage>().WithMany()
                        .HasForeignKey("MembershipPackageId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_package_permissions_membership_packages"),
                    j =>
                    {
                        j.HasKey("MembershipPackageId", "PermissionId").HasName("PK__package___85F69B762B246D8B");
                        j.ToTable("package_permissions");
                        j.IndexerProperty<int>("MembershipPackageId").HasColumnName("membership_package_id");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.PaymentTransactionId).HasName("PK__PaymentT__C22AEFE08701D796");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentId).HasMaxLength(100);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MembershipPackage).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.MembershipPackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransactions_MembershipPackages");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransactions_Users");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__permissi__E5331AFA6D1D57AE");

            entity.ToTable("permissions");

            entity.HasIndex(e => e.PermissionName, "UQ__permissi__81C0F5A26A3FC4A4").IsUnique();

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PermissionName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("permission_name");
        });

        modelBuilder.Entity<Reply>(entity =>
        {
            entity.HasKey(e => e.ReplyId).HasName("PK__replies__EE405698FB74777A");

            entity.ToTable("replies");

            entity.Property(e => e.ReplyId).HasColumnName("reply_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");

            entity.HasOne(d => d.Admin).WithMany(p => p.Replies)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_replies_admin");

            entity.HasOne(d => d.Feedback).WithMany(p => p.Replies)
                .HasForeignKey(d => d.FeedbackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_replies_feedbacks");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370FF2650AA0");

            entity.ToTable("users");

            entity.HasIndex(e => e.PhoneNumber, "UQ__users__A1936A6BE06256D0");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E6164D49DAE4A").IsUnique();

            entity.HasIndex(e => e.Uid, "UQ__users__DD701265737C6BD3").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.EmailActivation)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("unactivated")
                .HasColumnName("email_activation");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.MembershipPackageId).HasColumnName("membership_package_id");
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Uid)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("uid");

            entity.HasOne(d => d.MembershipPackage).WithMany(p => p.Users)
                .HasForeignKey(d => d.MembershipPackageId)
                .HasConstraintName("FK_users_membership_packages");
        });

        modelBuilder.Entity<UserMembership>(entity =>
        {
            entity.HasKey(e => e.UserMembershipId).HasName("PK__user_mem__E37A2534EAAEBDD0");

            entity.ToTable("user_memberships");

            entity.Property(e => e.UserMembershipId).HasColumnName("user_membership_id");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.MembershipPackageId).HasColumnName("membership_package_id");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MembershipPackage).WithMany(p => p.UserMemberships)
                .HasForeignKey(d => d.MembershipPackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_memberships_membership_packages");

            entity.HasOne(d => d.PaymentTransaction).WithMany(p => p.UserMemberships)
                .HasForeignKey(d => d.PaymentTransactionId)
                .HasConstraintName("FK_user_memberships_payment_transactions");

            entity.HasOne(d => d.User).WithMany(p => p.UserMemberships)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_memberships_users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}