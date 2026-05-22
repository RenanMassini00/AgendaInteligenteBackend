namespace Scheduler.Api.DTOs;

public record CatalogDashboardResponse(
    int ProductsCount,
    int VisibleProductsCount,
    int SoldUnitsCount,
    int StockUnitsCount,
    decimal TotalStockValue,
    string TotalStockValueFormatted,
    decimal TotalSoldValue,
    string TotalSoldValueFormatted,
    int ActiveProductsCount,
    int LowStockProductsCount,
    int OutOfStockProductsCount
);