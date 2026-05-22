using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("category")]
    public string? Category { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("original_price")]
    public decimal? OriginalPrice { get; set; }

    [Column("promotional_price")]
    public decimal? PromotionalPrice { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("sold_quantity")]
    public int SoldQuantity { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_sold")]
    public bool IsSold { get; set; } = false;

    [Column("is_featured")]
    public bool IsFeatured { get; set; } = false;

    [Column("whatsapp_message")]
    public string? WhatsAppMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}