namespace Scheduler.Api.DTOs;

public record ProductResponse(
    ulong Id,
    string Name,
    string? Category,
    string? Description,
    decimal Price,
    string PriceFormatted,
    decimal? OriginalPrice,
    string? OriginalPriceFormatted,
    decimal? PromotionalPrice,
    string? PromotionalPriceFormatted,
    decimal EffectivePrice,
    string EffectivePriceFormatted,
    string? ImageUrl,
    int StockQuantity,
    int SoldQuantity,
    bool IsActive,
    bool IsSold,
    bool IsFeatured,
    bool IsAvailablePublic,
    string? WhatsAppMessage
);

public record ProductCreateRequest(
    ulong UserId,
    string Name,
    string? Category,
    string? Description,
    decimal Price,
    decimal? OriginalPrice,
    decimal? PromotionalPrice,
    string? ImageUrl,
    int StockQuantity,
    bool IsFeatured,
    string? WhatsAppMessage
);

public record ProductUpdateRequest(
    ulong UserId,
    string Name,
    string? Category,
    string? Description,
    decimal Price,
    decimal? OriginalPrice,
    decimal? PromotionalPrice,
    string? ImageUrl,
    int StockQuantity,
    bool IsActive,
    bool IsSold,
    bool IsFeatured,
    string? WhatsAppMessage
);

public record ProductSaleRequest(
    int Quantity
);

public record PublicCatalogResponse(
    ulong UserId,
    string ProfessionalName,
    string? BusinessName,
    string? Specialty,
    string? PublicSlug,
    string? Phone,
    List<PublicCatalogProductResponse> Products
);

public record PublicCatalogProductResponse(
    ulong Id,
    string Name,
    string? Category,
    string? Description,
    decimal Price,
    string PriceFormatted,
    decimal? OriginalPrice,
    string? OriginalPriceFormatted,
    decimal? PromotionalPrice,
    string? PromotionalPriceFormatted,
    decimal EffectivePrice,
    string EffectivePriceFormatted,
    string? ImageUrl,
    int StockQuantity,
    bool IsFeatured,
    string? WhatsAppUrl
);