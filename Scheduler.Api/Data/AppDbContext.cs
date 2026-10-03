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
    public DbSet<AppointmentPayment> AppointmentPayments => Set<AppointmentPayment>();
    public DbSet<MercadoPagoAccount> MercadoPagoAccounts => Set<MercadoPagoAccount>();
    public DbSet<MercadoPagoOAuthState> MercadoPagoOAuthStates => Set<MercadoPagoOAuthState>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistory => Set<AppointmentStatusHistory>();
    public DbSet<AvailabilityDate> AvailabilityDates => Set<AvailabilityDate>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<WebPushSubscription> PushSubscriptions => Set<WebPushSubscription>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_users_email");
            entity.HasIndex(e => e.PublicSlug).IsUnique().HasDatabaseName("uq_users_public_slug");
        });

        modelBuilder.Entity<UserSetting>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserSettings)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WebPushSubscription>(entity =>
        {
            entity.HasIndex(x => x.EndpointHash)
                .IsUnique()
                .HasDatabaseName("uq_push_subscriptions_endpoint_hash");

            entity.HasIndex(x => new { x.UserId, x.IsActive })
                .HasDatabaseName("idx_push_subscriptions_user_active");

            entity.Property(x => x.EndpointHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.P256dh).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Auth).HasMaxLength(255).IsRequired();
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.DeviceName).HasMaxLength(120);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppNotification>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
                .HasDatabaseName("idx_app_notifications_user_read_created");
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ActionUrl).HasMaxLength(500);
            entity.Property(x => x.CalendarUrl).HasMaxLength(2000);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(e => e.Clients)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ImageUrl).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.ProductId, x.SortOrder })
                .HasDatabaseName("idx_product_images_product_order");

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
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

        modelBuilder.Entity<AppointmentPayment>(entity =>
        {
            entity.HasIndex(x => x.AppointmentId)
                .IsUnique()
                .HasDatabaseName("uq_appointment_payments_appointment_id");
            entity.HasIndex(x => x.PublicReference)
                .IsUnique()
                .HasDatabaseName("uq_appointment_payments_public_reference");
            entity.HasIndex(x => x.ProviderPaymentId)
                .IsUnique()
                .HasDatabaseName("uq_appointment_payments_provider_payment_id");
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.PublicReference).HasColumnType("char(36)");
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ProviderPaymentId).HasMaxLength(40);
            entity.Property(x => x.QrCode).HasColumnType("longtext");
            entity.Property(x => x.QrCodeBase64).HasColumnType("longtext");
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AccountOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Appointment>()
                .WithOne()
                .HasForeignKey<AppointmentPayment>(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MercadoPagoAccount>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.MercadoPagoUserId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.AccessTokenEncrypted).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.RefreshTokenEncrypted).HasColumnType("longtext").IsRequired();
            entity.HasOne<User>()
                .WithOne()
                .HasForeignKey<MercadoPagoAccount>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MercadoPagoOAuthState>(entity =>
        {
            entity.HasIndex(x => x.StateHash)
                .IsUnique()
                .HasDatabaseName("uq_mercado_pago_oauth_states_hash");
            entity.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("idx_mercado_pago_oauth_states_expires_at");
            entity.Property(x => x.StateHash).HasMaxLength(64).IsRequired();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(x => x.TeamOwnerUserId).HasColumnName("team_owner_user_id");

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.TeamOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
