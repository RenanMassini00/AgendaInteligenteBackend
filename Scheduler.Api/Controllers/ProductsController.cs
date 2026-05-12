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
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(products.Select(x => new ProductResponse(
            x.Id,
            x.Name,
            x.Description,
            x.Price,
            x.Price.ToString("C", culture),
            x.ImageUrl,
            x.StockQuantity,
            x.IsActive,
            x.IsSold
        )).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetById(ulong id, [FromQuery] ulong userId)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (product is null)
        {
            return NotFound(new ApiMessage("Produto não encontrado."));
        }

        return Ok(new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Price.ToString("C", culture),
            product.ImageUrl,
            product.StockQuantity,
            product.IsActive,
            product.IsSold
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessage>> Create([FromBody] ProductCreateRequest request)
    {
        if (request.UserId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiMessage("Nome do produto é obrigatório."));
        }

        if (request.Price < 0)
        {
            return BadRequest(new ApiMessage("Preço inválido."));
        }

        if (request.StockQuantity < 0)
        {
            return BadRequest(new ApiMessage("Estoque inválido."));
        }

        var product = new Product
        {
            UserId = request.UserId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            StockQuantity = request.StockQuantity,
            IsActive = true,
            IsSold = false,
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
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == request.UserId);

        if (product is null)
        {
            return NotFound(new ApiMessage("Produto não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiMessage("Nome do produto é obrigatório."));
        }

        if (request.Price < 0)
        {
            return BadRequest(new ApiMessage("Preço inválido."));
        }

        if (request.StockQuantity < 0)
        {
            return BadRequest(new ApiMessage("Estoque inválido."));
        }

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.StockQuantity = request.StockQuantity;
        product.IsActive = request.IsActive;
        product.IsSold = request.IsSold;
        product.WhatsAppMessage = string.IsNullOrWhiteSpace(request.WhatsAppMessage) ? null : request.WhatsAppMessage.Trim();
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Produto atualizado com sucesso."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id, [FromQuery] ulong userId)
    {
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
}