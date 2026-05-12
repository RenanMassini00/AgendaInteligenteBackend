using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

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
                !x.IsSold &&
                x.StockQuantity > 0)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var phone = user.Phone ?? "";
        var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());

        var productResponses = products.Select(product =>
        {
            var message = string.IsNullOrWhiteSpace(product.WhatsAppMessage)
                ? $"Olá! Tenho interesse no produto: {product.Name}"
                : $"{product.WhatsAppMessage} | Produto: {product.Name}";

            var whatsappUrl = $"https://wa.me/55{normalizedPhone}?text={Uri.EscapeDataString(message)}";

            return new PublicCatalogProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Price.ToString("C", culture),
                product.ImageUrl,
                product.StockQuantity,
                whatsappUrl
            );
        }).ToList();

        return Ok(new PublicCatalogResponse(
            user.Id,
            user.FullName,
            user.BusinessName,
            user.Specialty,
            user.PublicSlug,
            user.Phone,
            productResponses
        ));
    }
}