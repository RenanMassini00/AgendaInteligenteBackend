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
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();
    public DbSet<Product> Products => Set<Product>();
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
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.OwnerName).HasColumnName("owner_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(x => x.Document).HasColumnName("document").HasMaxLength(30);
            entity.Property(x => x.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
            entity.Property(x => x.PublicSlug).HasColumnName("public_slug").HasMaxLength(120);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(x => x.MonthlyFee).HasColumnName("monthly_fee").HasColumnType("decimal(10,2)");
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<BillingRecord>(entity =>
        {
            entity.ToTable("billing_records");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CompanyId).HasColumnName("company_id");
            entity.Property(x => x.ReferenceMonth).HasColumnName("reference_month").HasMaxLength(7).IsRequired();
            entity.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)");
            entity.Property(x => x.DueDate).HasColumnName("due_date");
            entity.Property(x => x.PaidAt).HasColumnName("paid_at");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
            entity.Property(x => x.Notes).HasColumnName("notes");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.Company)
                .WithMany(x => x.BillingRecords)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.CompanyId).HasColumnName("company_id");

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
