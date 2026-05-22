using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/catalog-dashboard")]
public class CatalogDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogDashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<CatalogDashboardResponse>> Get([FromQuery] ulong userId)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync();

        decimal GetEffectivePrice(Entities.Product product)
        {
            return product.PromotionalPrice.HasValue && product.PromotionalPrice.Value > 0
                ? product.PromotionalPrice.Value
                : product.Price;
        }

        var productsCount = products.Count;
        var visibleProductsCount = products.Count(x => x.StockQuantity > 0);
        var soldUnitsCount = products.Sum(x => x.SoldQuantity);
        var stockUnitsCount = products.Sum(x => x.StockQuantity);
        var totalStockValue = products.Sum(x => x.StockQuantity * GetEffectivePrice(x));
        var totalSoldValue = products.Sum(x => x.SoldQuantity * GetEffectivePrice(x));
        var activeProductsCount = products.Count(x => x.StockQuantity > 0);
        var lowStockProductsCount = products.Count(x => x.StockQuantity > 0 && x.StockQuantity <= 3);
        var outOfStockProductsCount = products.Count(x => x.StockQuantity == 0);

        return Ok(new CatalogDashboardResponse(
            productsCount,
            visibleProductsCount,
            soldUnitsCount,
            stockUnitsCount,
            totalStockValue,
            totalStockValue.ToString("C", culture),
            totalSoldValue,
            totalSoldValue.ToString("C", culture),
            activeProductsCount,
            lowStockProductsCount,
            outOfStockProductsCount
        ));
    }
}