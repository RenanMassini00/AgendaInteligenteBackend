using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/public/catalog")]
public class PublicCatalogController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicCatalogController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicCatalogResponse>> GetBySlug(string slug)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.PublicSlug == slug &&
                x.Role == "professional" &&
                x.IsActive);

        if (user is null)
        {
            return NotFound(new ApiMessage("Catálogo não encontrado."));
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(x =>
                x.UserId == user.Id &&
                x.IsActive &&
                x.StockQuantity > 0)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var normalizedPhone = new string((user.Phone ?? string.Empty).Where(char.IsDigit).ToArray());

        var productResponses = products.Select(product =>
        {
            var effectivePrice = GetEffectivePrice(product);

            string? whatsAppUrl = null;

            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var message = string.IsNullOrWhiteSpace(product.WhatsAppMessage)
                    ? $"Olá! Tenho interesse no produto: {product.Name}"
                    : $"{product.WhatsAppMessage} | Produto: {product.Name}";

                whatsAppUrl = $"https://wa.me/55{normalizedPhone}?text={Uri.EscapeDataString(message)}";
            }

            return new PublicCatalogProductResponse(
                product.Id,
                product.Name,
                product.Category,
                product.Description,
                product.Price,
                product.Price.ToString("C", culture),
                product.OriginalPrice,
                product.OriginalPrice.HasValue ? product.OriginalPrice.Value.ToString("C", culture) : null,
                product.PromotionalPrice,
                product.PromotionalPrice.HasValue ? product.PromotionalPrice.Value.ToString("C", culture) : null,
                effectivePrice,
                effectivePrice.ToString("C", culture),
                product.ImageUrl,
                product.StockQuantity,
                product.IsFeatured,
                whatsAppUrl
            );
        }).ToList();

        var branding = await GetBrandingAsync(user.Id);

        return Ok(new PublicCatalogResponse(
            user.Id,
            user.FullName,
            user.BusinessName,
            user.Specialty,
            user.PublicSlug,
            user.Phone,
            productResponses,
            branding.ThemeMode,
            branding.AccentColor,
            branding.LogoUrl
        ));
    }

    private static decimal GetEffectivePrice(Product product)
    {
        return product.PromotionalPrice.HasValue && product.PromotionalPrice.Value > 0
            ? product.PromotionalPrice.Value
            : product.Price;
    }

    private async Task<BrandingSnapshot> GetBrandingAsync(ulong userId)
    {
        var settings = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return new BrandingSnapshot(
            settings?.ThemeMode ?? "light",
            settings?.AccentColor ?? "blue",
            settings?.LogoUrl
        );
    }

    private sealed record BrandingSnapshot(string ThemeMode, string AccentColor, string? LogoUrl);
}
