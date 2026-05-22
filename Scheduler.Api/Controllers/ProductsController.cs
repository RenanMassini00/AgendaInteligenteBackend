using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetAll([FromQuery] ulong userId)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(products.Select(product => MapProductResponse(product, culture)).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetById(ulong id, [FromQuery] ulong userId)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (product is null)
        {
            return NotFound(new ApiMessage("Produto não encontrado."));
        }

        return Ok(MapProductResponse(product, culture));
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessage>> Create([FromBody] ProductCreateRequest request)
    {
        var validationError = ValidateRequest(
            request.Name,
            request.Price,
            request.OriginalPrice,
            request.PromotionalPrice,
            request.StockQuantity
        );

        if (validationError is not null)
        {
            return validationError;
        }

        var product = new Product
        {
            UserId = request.UserId,
            Name = request.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            PromotionalPrice = request.PromotionalPrice,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            StockQuantity = request.StockQuantity,
            SoldQuantity = 0,
            IsActive = true,
            IsSold = request.StockQuantity == 0,
            IsFeatured = request.IsFeatured,
            WhatsAppMessage = string.IsNullOrWhiteSpace(request.WhatsAppMessage) ? null : request.WhatsAppMessage.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Produto cadastrado com sucesso."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiMessage>> Update(ulong id, [FromBody] ProductUpdateRequest request)
    {
        if (request.UserId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var validationError = ValidateRequest(
            request.Name,
            request.Price,
            request.OriginalPrice,
            request.PromotionalPrice,
            request.StockQuantity
        );

        if (validationError is not null)
        {
            return validationError;
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == request.UserId);

        if (product is null)
        {
            return NotFound(new ApiMessage("Produto não encontrado."));
        }

        product.Name = request.Name.Trim();
        product.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.OriginalPrice = request.OriginalPrice;
        product.PromotionalPrice = request.PromotionalPrice;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.StockQuantity = request.StockQuantity;
        product.IsActive = request.IsActive;
        product.IsFeatured = request.IsFeatured;
        product.WhatsAppMessage = string.IsNullOrWhiteSpace(request.WhatsAppMessage) ? null : request.WhatsAppMessage.Trim();
        product.IsSold = product.StockQuantity == 0 || request.IsSold;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Produto atualizado com sucesso."));
    }

    [HttpPost("{id}/register-sale")]
    public async Task<ActionResult<ApiMessage>> RegisterSale(
        ulong id,
        [FromQuery] ulong userId,
        [FromBody] ProductSaleRequest request)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new ApiMessage("A quantidade vendida deve ser maior que zero."));
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (product is null)
        {
            return NotFound(new ApiMessage("Produto não encontrado."));
        }

        if (!product.IsActive)
        {
            return BadRequest(new ApiMessage("O produto está inativo."));
        }

        if (product.StockQuantity < request.Quantity)
        {
            return BadRequest(new ApiMessage("Estoque insuficiente para registrar essa venda."));
        }

        product.StockQuantity -= request.Quantity;
        product.SoldQuantity += request.Quantity;
        product.IsSold = product.StockQuantity == 0;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage(
            $"Venda registrada com sucesso. Estoque restante: {product.StockQuantity}."
        ));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id, [FromQuery] ulong userId)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (product is null)
        {
            return NotFound(new ApiMessage("Produto não encontrado."));
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Produto removido com sucesso."));
    }

    private ActionResult<ApiMessage>? ValidateRequest(
        string name,
        decimal price,
        decimal? originalPrice,
        decimal? promotionalPrice,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new ApiMessage("Nome do produto é obrigatório."));
        }

        if (price < 0)
        {
            return BadRequest(new ApiMessage("Preço inválido."));
        }

        if (originalPrice.HasValue && originalPrice.Value < 0)
        {
            return BadRequest(new ApiMessage("Preço original inválido."));
        }

        if (promotionalPrice.HasValue && promotionalPrice.Value < 0)
        {
            return BadRequest(new ApiMessage("Preço promocional inválido."));
        }

        if (originalPrice.HasValue &&
            promotionalPrice.HasValue &&
            promotionalPrice.Value > originalPrice.Value)
        {
            return BadRequest(new ApiMessage("O preço promocional não pode ser maior que o preço original."));
        }

        if (stockQuantity < 0)
        {
            return BadRequest(new ApiMessage("Quantidade em estoque inválida."));
        }

        return null;
    }

    private static ProductResponse MapProductResponse(Product product, CultureInfo culture)
    {
        var effectivePrice = GetEffectivePrice(product);

        return new ProductResponse(
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
            product.SoldQuantity,
            product.IsActive,
            product.IsSold,
            product.IsFeatured,
            product.IsActive && product.StockQuantity > 0,
            product.WhatsAppMessage
        );
    }

    private static decimal GetEffectivePrice(Product product)
    {
        return product.PromotionalPrice.HasValue && product.PromotionalPrice.Value > 0
            ? product.PromotionalPrice.Value
            : product.Price;
    }
}