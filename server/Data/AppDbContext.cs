using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using EmployeeHRMS.Api.Models;

namespace EmployeeHRMS.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<InterviewQuestion> InterviewQuestions { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // Primary Key — Employee dùng EmployeeId (không phải Id mặc định)
            // ============================================================
            modelBuilder.Entity<Employee>()
                .HasKey(e => e.EmployeeId);

            // ============================================================
            // Relationships — Thiết lập Foreign Key và Delete Behavior
            // ============================================================

            // Employee → Department (Many-to-One)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobPosting → Department (Many-to-One)
            modelBuilder.Entity<JobPosting>()
                .HasOne(j => j.Department)
                .WithMany(d => d.JobPostings)
                .HasForeignKey(j => j.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Application → Candidate (Many-to-One)
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Candidate)
                .WithMany(c => c.Applications)
                .HasForeignKey(a => a.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Application → JobPosting (Many-to-One)
            modelBuilder.Entity<Application>()
                .HasOne(a => a.JobPosting)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobPostingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Interview → Application (Many-to-One)
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Application)
                .WithMany()
                .HasForeignKey(i => i.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Interview → ApplicationUser/Interviewer (Optional Many-to-One)
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Interviewer)
                .WithMany()
                .HasForeignKey(i => i.InterviewerId)
                .OnDelete(DeleteBehavior.SetNull);

            // InterviewQuestion → Interview (Many-to-One, Cascade delete)
            modelBuilder.Entity<InterviewQuestion>()
                .HasOne(q => q.Interview)
                .WithMany(i => i.Questions)
                .HasForeignKey(q => q.InterviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser → Employee (Optional One-to-One)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Candidate → ApplicationUser (Optional Many-to-One)
            modelBuilder.Entity<Candidate>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // RefreshToken → ApplicationUser (Many-to-One, Cascade Delete)
            modelBuilder.Entity<RefreshToken>()
                .HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification → ApplicationUser (Many-to-One, Cascade Delete)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // Unique Constraints & Entity Configurations
            // ============================================================
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.EmailVerificationTokenHash).HasMaxLength(64);
                entity.Property(u => u.IsEmailVerified).HasDefaultValue(false);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(r => r.TokenHash).IsUnique();
                entity.HasIndex(r => r.UserId)
                    .HasFilter("\"RevokedAt\" IS NULL")
                    .HasDatabaseName("IX_RefreshTokens_UserId_Active");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
                entity.Property(n => n.TemplateKey).HasMaxLength(50).IsRequired().HasDefaultValue("LEGACY_MESSAGE");
                var payloadProp = entity.Property(n => n.Payload)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
                payloadProp.Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
                    c => c.Aggregate(0, (a, p) => HashCode.Combine(a, p.Key.GetHashCode(), p.Value.GetHashCode())),
                    c => new Dictionary<string, string>(c)));
                entity.Property(n => n.Message).HasMaxLength(1000).IsRequired(false);
                entity.Property(n => n.RelatedEntity).HasMaxLength(50);
                entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                    .IsDescending(false, false, true)
                    .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");
            });

            // ============================================================
            // Decimal Precision — Tránh mất dữ liệu số thập phân
            // ============================================================
            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Application>()
                .Property(a => a.AiMatchScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Interview>()
                .Property(i => i.AiOverallScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<InterviewQuestion>()
                .Property(q => q.AiQuestionScore)
                .HasPrecision(5, 2);

            // ============================================================
            // Seed Data — Admin user mặc định
            // ============================================================
            var adminUser = new ApplicationUser("admin@hrms.com", string.Empty, UserRole.Admin);
            var hasher = new PasswordHasher<ApplicationUser>();
            var adminPasswordHash = hasher.HashPassword(adminUser, "Admin@123");

            modelBuilder.Entity<ApplicationUser>().HasData(new
            {
                Id = 1,
                Email = "admin@hrms.com",
                PasswordHash = adminPasswordHash,
                Role = UserRole.Admin,
                EmployeeId = (int?)null,
                IsEmailVerified = true
            });
        }
    }
}
