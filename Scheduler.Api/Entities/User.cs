using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("business_name")]
    public string? BusinessName { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("specialty")]
    public string? Specialty { get; set; }

    [Column("timezone")]
    public string Timezone { get; set; } = "America/Sao_Paulo";

    [Column("role")]
    public string Role { get; set; } = "professional";

    [Column("professional_user_id")]
    public ulong? ProfessionalUserId { get; set; }

    [Column("client_id")]
    public ulong? ClientId { get; set; }

    [Column("public_slug")]
    public string? PublicSlug { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Client> Clients { get; set; } = [];
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
