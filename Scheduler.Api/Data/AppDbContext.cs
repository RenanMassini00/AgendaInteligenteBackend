using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<WeeklyAvailability> WeeklyAvailabilities => Set<WeeklyAvailability>();
    public DbSet<BlockedPeriod> BlockedPeriods => Set<BlockedPeriod>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistory => Set<AppointmentStatusHistory>();
    public DbSet<AvailabilityDate> AvailabilityDates => Set<AvailabilityDate>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_users_email");
            entity.HasIndex(e => e.PublicSlug).IsUnique().HasDatabaseName("uq_users_public_slug");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("uq_user_settings_user_id");
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<UserSetting>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(e => e.Clients)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(e => e.Services)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklyAvailability>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlockedPeriod>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Client)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Service)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppointmentStatusHistory>(entity =>
        {
            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvailabilityDate>(entity =>
        {
            entity.ToTable("availability_dates");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.UserId)
                .HasColumnName("user_id");

            entity.Property(x => x.AvailableDate)
                .HasColumnName("available_date")
                .HasColumnType("date");

            entity.Property(x => x.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("time");

            entity.Property(x => x.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("time");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime");

            entity.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
