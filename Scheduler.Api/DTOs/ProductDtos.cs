namespace Scheduler.Api.DTOs;

public record ProductResponse(
    ulong Id,
    string Name,
    string? Description,
    decimal Price,
    string PriceFormatted,
    string? ImageUrl,
    int StockQuantity,
    bool IsActive,
    bool IsSold
);

public record ProductCreateRequest(
    ulong UserId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int StockQuantity,
    string? WhatsAppMessage
);

public record ProductUpdateRequest(
    ulong UserId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    int StockQuantity,
    bool IsActive,
    bool IsSold,
    string? WhatsAppMessage
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
    string? Description,
    decimal Price,
    string PriceFormatted,
    string? ImageUrl,
    int StockQuantity,
    string WhatsAppUrl
);