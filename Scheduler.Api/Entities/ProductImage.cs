using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("product_images")]
public class ProductImage
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("product_id")]
    public ulong ProductId { get; set; }

    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
