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

    public virtual DbSet<DeviationAnalysis> DeviationAnalyses { get; set; }

    public virtual DbSet<Faq> Faqs { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<GrowthIndicator> GrowthIndicators { get; set; }

    public virtual DbSet<MembershipPackage> MembershipPackages { get; set; }

    public virtual DbSet<Paymenttransaction> Paymenttransactions { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Reply> Replies { get; set; }

    public virtual DbSet<Teethingrecord> Teethingrecords { get; set; }

    public virtual DbSet<Tooth> Teeth { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserMembership> UserMemberships { get; set; }

    public virtual DbSet<VaccinationSchedule> VaccinationSchedules { get; set; }

    public virtual DbSet<Vaccine> Vaccines { get; set; }

    public virtual DbSet<Vaccinerecord> Vaccinerecords { get; set; }

    public virtual DbSet<WhoGrowthStandard> WhoGrowthStandards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogContent>(entity =>
        {
            entity.HasKey(e => e.BlogContentId).HasName("blog_contents_pkey");

            entity.ToTable("blog_contents");

            entity.Property(e => e.BlogContentId).HasColumnName("blog_content_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Likes)
                .HasDefaultValue(0)
                .HasColumnName("likes");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.ThumbnailUrl)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("thumbnail_url");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Views)
                .HasDefaultValue(0)
                .HasColumnName("views");

            entity.HasOne(d => d.Admin).WithMany(p => p.BlogContents)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_blog_contents_admin");

            entity.HasMany(d => d.Categories).WithMany(p => p.BlogContents)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_blog_categories_categories"),
                    l => l.HasOne<BlogContent>().WithMany()
                        .HasForeignKey("BlogContentId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_blog_categories_blog_contents"),
                    j =>
                    {
                        j.HasKey("BlogContentId", "CategoryId").HasName("blog_categories_pkey");
                        j.ToTable("blog_categories");
                        j.IndexerProperty<int>("BlogContentId").HasColumnName("blog_content_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("category_name");
        });

        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(e => e.ChildrenId).HasName("children_pkey");

            entity.ToTable("children");

            entity.Property(e => e.ChildrenId).HasColumnName("children_id");
            entity.Property(e => e.Allergies)
                .HasMaxLength(255)
                .HasColumnName("allergies");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .HasColumnName("avatar");
            entity.Property(e => e.BloodType)
                .HasMaxLength(10)
                .HasColumnName("blood_type");
            entity.Property(e => e.ChronicConditions)
                .HasMaxLength(255)
                .HasColumnName("chronic_conditions");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(50)
                .HasColumnName("gender");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasColumnName("status");

            entity.HasOne(d => d.Member).WithMany(p => p.Children)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_children_users");
        });

        modelBuilder.Entity<ConsultationNote>(entity =>
        {
            entity.HasKey(e => e.ConsultationNoteId).HasName("consultation_notes_pkey");

            entity.ToTable("consultation_notes");

            entity.Property(e => e.ConsultationNoteId).HasColumnName("consultation_note_id");
            entity.Property(e => e.ChildrenId).HasColumnName("children_id");
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.RecordTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("record_time");

            entity.HasOne(d => d.Children).WithMany(p => p.ConsultationNotes)
                .HasForeignKey(d => d.ChildrenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_consultation_notes_children");

            entity.HasOne(d => d.Doctor).WithMany(p => p.ConsultationNoteDoctors)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_consultation_notes_doctor");

            entity.HasOne(d => d.Member).WithMany(p => p.ConsultationNoteMembers)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_consultation_notes_member");
        });

        modelBuilder.Entity<DeviationAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deviation_analysis_pkey");

            entity.ToTable("deviation_analysis");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ComputedValue)
                .HasPrecision(5, 2)
                .HasColumnName("computed_value");
            entity.Property(e => e.DeviationPercentage)
                .HasPrecision(5, 2)
                .HasColumnName("deviation_percentage");
            entity.Property(e => e.GrowthRecordId).HasColumnName("growth_record_id");

            entity.HasOne(d => d.GrowthRecord).WithMany(p => p.DeviationAnalyses)
                .HasForeignKey(d => d.GrowthRecordId)
                .HasConstraintName("fk_deviation_analysis_growth_indicators");
        });

        modelBuilder.Entity<Faq>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("faq_pkey");

            entity.ToTable("faq");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Answer)
                .HasMaxLength(255)
                .HasColumnName("answer");
            entity.Property(e => e.Question)
                .HasMaxLength(255)
                .HasColumnName("question");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("feedbacks_pkey");

            entity.ToTable("feedbacks");

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Rating).HasColumnName("rating");

            entity.HasOne(d => d.Member).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_feedbacks_users");
        });

        modelBuilder.Entity<GrowthIndicator>(entity =>
        {
            entity.HasKey(e => e.GrowthIndicatorsId).HasName("growth_indicators_pkey");

            entity.ToTable("growth_indicators");

            entity.Property(e => e.GrowthIndicatorsId).HasColumnName("growth_indicators_id");
            entity.Property(e => e.Bmi)
                .HasPrecision(10, 4)
                .HasColumnName("bmi");
            entity.Property(e => e.ChildrenId).HasColumnName("children_id");
            entity.Property(e => e.Height)
                .HasPrecision(10, 4)
                .HasColumnName("height");
            entity.Property(e => e.RecordTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("record_time");
            entity.Property(e => e.Weight)
                .HasPrecision(10, 4)
                .HasColumnName("weight");

            entity.HasOne(d => d.Children).WithMany(p => p.GrowthIndicators)
                .HasForeignKey(d => d.ChildrenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_growth_indicators_children");
        });

        modelBuilder.Entity<MembershipPackage>(entity =>
        {
            entity.HasKey(e => e.MembershipPackageId).HasName("membership_packages_pkey");

            entity.ToTable("membership_packages");

            entity.HasIndex(e => e.MembershipPackageName, "membership_packages_membership_package_name_key").IsUnique();

            entity.Property(e => e.MembershipPackageId).HasColumnName("membership_package_id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_time");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.MembershipPackageName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("membership_package_name");
            entity.Property(e => e.PercentDiscount).HasColumnName("percent_discount");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.ValidityPeriod).HasColumnName("validity_period");
            entity.Property(e => e.YearlyPrice)
                .HasPrecision(18, 2)
                .HasColumnName("yearly_price");

            entity.HasMany(d => d.Permissions).WithMany(p => p.MembershipPackages)
                .UsingEntity<Dictionary<string, object>>(
                    "PackagePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_package_permissions_permissions"),
                    l => l.HasOne<MembershipPackage>().WithMany()
                        .HasForeignKey("MembershipPackageId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_package_permissions_membership_packages"),
                    j =>
                    {
                        j.HasKey("MembershipPackageId", "PermissionId").HasName("package_permissions_pkey");
                        j.ToTable("package_permissions");
                        j.IndexerProperty<int>("MembershipPackageId").HasColumnName("membership_package_id");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<Paymenttransaction>(entity =>
        {
            entity.HasKey(e => e.Paymenttransactionid).HasName("paymenttransactions_pkey");

            entity.ToTable("paymenttransactions");

            entity.Property(e => e.Paymenttransactionid).HasColumnName("paymenttransactionid");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Membershippackageid).HasColumnName("membershippackageid");
            entity.Property(e => e.Paymentid)
                .HasMaxLength(100)
                .HasColumnName("paymentid");
            entity.Property(e => e.PreviousMembershipPackageName)
                .HasMaxLength(255)
                .HasColumnName("previous_membership_package_name");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Transactiondate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("transactiondate");
            entity.Property(e => e.UserMembershipId).HasColumnName("user_membership_id");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Membershippackage).WithMany(p => p.Paymenttransactions)
                .HasForeignKey(d => d.Membershippackageid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_paymenttransactions_membershippackages");

            entity.HasOne(d => d.UserMembership).WithMany(p => p.Paymenttransactions)
                .HasForeignKey(d => d.UserMembershipId)
                .HasConstraintName("fk_paymenttransactions_usermembership");

            entity.HasOne(d => d.User).WithMany(p => p.Paymenttransactions)
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_paymenttransactions_users");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("permissions_pkey");

            entity.ToTable("permissions");

            entity.HasIndex(e => e.PermissionName, "permissions_permission_name_key").IsUnique();

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PermissionName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("permission_name");
        });

        modelBuilder.Entity<Reply>(entity =>
        {
            entity.HasKey(e => e.ReplyId).HasName("replies_pkey");

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
                .HasConstraintName("fk_replies_admin");

            entity.HasOne(d => d.Feedback).WithMany(p => p.Replies)
                .HasForeignKey(d => d.FeedbackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_replies_feedbacks");
        });

        modelBuilder.Entity<Teethingrecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("teethingrecord_pkey");

            entity.ToTable("teethingrecord");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChildId).HasColumnName("child_id");
            entity.Property(e => e.EruptionDate).HasColumnName("eruption_date");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.RecordTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("record_time");
            entity.Property(e => e.ToothId).HasColumnName("tooth_id");

            entity.HasOne(d => d.Child).WithMany(p => p.Teethingrecords)
                .HasForeignKey(d => d.ChildId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_teethingrecord_children");

            entity.HasOne(d => d.Tooth).WithMany(p => p.Teethingrecords)
                .HasForeignKey(d => d.ToothId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_teethingrecord_tooth");
        });

        modelBuilder.Entity<Tooth>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tooth_pkey");

            entity.ToTable("tooth");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NumberOfTeeth).HasColumnName("number_of_teeth");
            entity.Property(e => e.TeethingPeriod).HasColumnName("teething_period");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .HasColumnName("avatar");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailActivation)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValueSql("'unactivated'::character varying")
                .HasColumnName("email_activation");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Hospital)
                .HasMaxLength(255)
                .HasColumnName("hospital");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(255)
                .HasColumnName("license_number");
            entity.Property(e => e.MembershipPackageId).HasColumnName("membership_package_id");
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.Specialization)
                .HasMaxLength(255)
                .HasColumnName("specialization");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .HasColumnName("state");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Uid)
                .HasMaxLength(255)
                .HasColumnName("uid");
            entity.Property(e => e.Zipcode)
                .HasMaxLength(50)
                .HasColumnName("zipcode");

            entity.HasOne(d => d.MembershipPackage).WithMany(p => p.Users)
                .HasForeignKey(d => d.MembershipPackageId)
                .HasConstraintName("fk_users_membership_packages");
        });

        modelBuilder.Entity<UserMembership>(entity =>
        {
            entity.HasKey(e => e.UserMembershipId).HasName("user_memberships_pkey");

            entity.ToTable("user_memberships");

            entity.Property(e => e.UserMembershipId).HasColumnName("user_membership_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.MembershipPackageId).HasColumnName("membership_package_id");
            entity.Property(e => e.Paymenttransactionid).HasColumnName("paymenttransactionid");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MembershipPackage).WithMany(p => p.UserMemberships)
                .HasForeignKey(d => d.MembershipPackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_memberships_membership_packages");

            entity.HasOne(d => d.Paymenttransaction).WithMany(p => p.UserMemberships)
                .HasForeignKey(d => d.Paymenttransactionid)
                .HasConstraintName("fk_usermemberships_paymenttransactions");

            entity.HasOne(d => d.User).WithMany(p => p.UserMemberships)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_memberships_users");
        });

        modelBuilder.Entity<VaccinationSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vaccination_schedule_pkey");

            entity.ToTable("vaccination_schedule");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RecommendedAgeMonths).HasColumnName("recommended_age_months");
            entity.Property(e => e.VaccineId).HasColumnName("vaccine_id");

            entity.HasOne(d => d.Vaccine).WithMany(p => p.VaccinationSchedules)
                .HasForeignKey(d => d.VaccineId)
                .HasConstraintName("fk_vaccination_schedule_vaccine");
        });

        modelBuilder.Entity<Vaccine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vaccine_pkey");

            entity.ToTable("vaccine");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DosesRequired).HasColumnName("doses_required");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Vaccinerecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vaccinerecord_pkey");

            entity.ToTable("vaccinerecord");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdministeredDate).HasColumnName("administered_date");
            entity.Property(e => e.ChildId).HasColumnName("child_id");
            entity.Property(e => e.Dose).HasColumnName("dose");
            entity.Property(e => e.NextDoseDate).HasColumnName("next_dose_date");
            entity.Property(e => e.VaccineId).HasColumnName("vaccine_id");

            entity.HasOne(d => d.Child).WithMany(p => p.Vaccinerecords)
                .HasForeignKey(d => d.ChildId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_vaccinerecord_children");

            entity.HasOne(d => d.Vaccine).WithMany(p => p.Vaccinerecords)
                .HasForeignKey(d => d.VaccineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_vaccinerecord_vaccine");
        });

        modelBuilder.Entity<WhoGrowthStandard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("who_growth_standards_pkey");

            entity.ToTable("who_growth_standards");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgeMonths).HasColumnName("age_months");
            entity.Property(e => e.BmiAvg)
                .HasPrecision(10, 4)
                .HasColumnName("bmi_avg");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.HeightAvg)
                .HasPrecision(10, 4)
                .HasColumnName("height_avg");
            entity.Property(e => e.WeightAvg)
                .HasPrecision(10, 4)
                .HasColumnName("weight_avg");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}