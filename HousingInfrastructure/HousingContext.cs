using System;
using System.Collections.Generic;
using HousingDomain.Models;
using Microsoft.EntityFrameworkCore;

namespace HousingInfrastructure;

public partial class HousingContext : DbContext
{
    public HousingContext()
    {
    }

    public HousingContext(DbContextOptions<HousingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Availability> Availabilities { get; set; }

    public virtual DbSet<BookingRequest> BookingRequests { get; set; }

    public virtual DbSet<Housing> Housings { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=labdb;Username=mary;Password=mary");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Availability>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Availability_pkey");

            entity.ToTable("Availability");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateFrom).HasColumnName("date_from");
            entity.Property(e => e.DateTo).HasColumnName("date_to");
            entity.Property(e => e.HousingId).HasColumnName("housing_id");

            entity.HasOne(d => d.Housing).WithMany(p => p.Availabilities)
                .HasForeignKey(d => d.HousingId)
                .HasConstraintName("fk_booking_housing");
        });

        modelBuilder.Entity<BookingRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("BookingRequests_pkey");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateFrom).HasColumnName("date_from");
            entity.Property(e => e.DateTo).HasColumnName("date_to");
            entity.Property(e => e.HousingId).HasColumnName("housing_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Housing).WithMany(p => p.BookingRequests)
                .HasForeignKey(d => d.HousingId)
                .HasConstraintName("fk_booking_housing");

            entity.HasOne(d => d.User).WithMany(p => p.BookingRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_booking_user");
        });

        modelBuilder.Entity<Housing>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("housing_pkey");

            entity.ToTable("housing");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(150)
                .HasColumnName("address");
            entity.Property(e => e.Area)
                .HasPrecision(5, 2)
                .HasColumnName("area");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Rooms).HasColumnName("rooms");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Profiles_pkey");

            entity.HasIndex(e => e.UserId, "unique_user_profile").IsUnique();

            entity.Property(e => e.CleanLevel)
                .HasColumnType("character varying")
                .HasColumnName("Clean_level");
            entity.Property(e => e.Guests).HasColumnType("character varying");
            entity.Property(e => e.NoiseLevel)
                .HasColumnType("character varying")
                .HasColumnName("Noise_level");
            entity.Property(e => e.SleepMode)
                .HasColumnType("character varying")
                .HasColumnName("Sleep_mode");
            entity.Property(e => e.Smoking).HasColumnType("character varying");
            entity.Property(e => e.PreferredGender)
                .HasColumnType("character varying")
                .HasColumnName("Preferred_gender");
            entity.Property(e => e.UserId).HasColumnName("User_Id");
            entity.Property(e => e.UserId).HasColumnName("User_id");

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("fk_profile_user");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reviews_pkey");

            entity.ToTable("reviews");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.HousingId).HasColumnName("housing_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Housing).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.HousingId)
                .HasConstraintName("fk_reviews_housing");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_reviews_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.Property(e => e.Gender).HasMaxLength(6);
            entity.Property(e => e.IsOwnerApproved).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasDefaultValue("Renter");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
