using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CounterWeb.Models;

public partial class CounterDbContext : DbContext
{
    public CounterDbContext()
    {
    }

    public CounterDbContext(DbContextOptions<CounterDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CompletedTask> CompletedTasks { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Personalization> Personalizations { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<Theme> Themes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserCourse> UserCourses { get; set; }

    public virtual DbSet<UserRegInfo> UserRegInfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=MYPC;Database=CounterDB;Trusted_Connection=True;Trust Server Certificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompletedTask>(entity =>
        {
            entity.ToTable("CompletedTask");

            entity.Property(e => e.Solution)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.Task).WithMany(p => p.CompletedTasks)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CompletedTask_Task");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK_Courses");

            entity.ToTable("Course");

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ZoomLink)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguageId).HasName("PK_Languages");

            entity.ToTable("Language");

            entity.Property(e => e.LanguageId).ValueGeneratedNever();
            entity.Property(e => e.English)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Russian)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Ukranian)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Personalization>(entity =>
        {
            entity.HasKey(e => e.PersonalizationId).HasName("PK__Personal__69D4B79E33CAAC6B");

            entity.ToTable("Personalization");

            entity.HasIndex(e => e.LanguageId, "UQ__Personal__B93855AA20350C83").IsUnique();

            entity.HasIndex(e => e.ThemeId, "UQ__Personal__FBB3E4D8AE710C40").IsUnique();

            entity.Property(e => e.PersonalizationId).ValueGeneratedNever();

            entity.HasOne(d => d.Language).WithOne(p => p.Personalization)
                .HasForeignKey<Personalization>(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Personali__Langu__6E01572D");

            entity.HasOne(d => d.Theme).WithOne(p => p.Personalization)
                .HasForeignKey<Personalization>(d => d.ThemeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Personali__Theme__6EF57B66");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK_Tasks");

            entity.ToTable("Task");

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Course).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tasks_Courses");
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.ToTable("Theme");

            entity.Property(e => e.DarkThemeSettings)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WhiteThemeSettings)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserInfo__1788CC4C3EBD089B");

            entity.ToTable("User");

            entity.HasIndex(e => e.RegInfoId, "UQ__UserInfo__8A8A243B3DDD2D86").IsUnique();

            entity.HasIndex(e => e.PersonalizationId, "UQ__UserInfo__964922B4C701DB01").IsUnique();

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.EmailAddr)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Personalization).WithOne(p => p.User)
                .HasForeignKey<User>(d => d.PersonalizationId)
                .HasConstraintName("FK__UserInfo__Person__06CD04F7");
        });

        modelBuilder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(e => e.UserCourseId).HasName("PK_UserNCourse");

            entity.ToTable("UserCourse");

            entity.HasOne(d => d.Course).WithMany(p => p.UserCourses)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__UserNCour__Cours__09A971A2");

            entity.HasOne(d => d.User).WithMany(p => p.UserCourses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserNCourse_UserInfo");
        });

        modelBuilder.Entity<UserRegInfo>(entity =>
        {
            entity.HasKey(e => e.RegInformationId).HasName("PK__UserRegI__8A8A243AB5AEBCB2");

            entity.ToTable("UserRegInfo");

            entity.Property(e => e.RegInformationId).ValueGeneratedNever();
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
