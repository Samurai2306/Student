using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Dtos;

namespace ProductionSystem.Controllers.Api;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    private readonly ProductionContext _context;

    public ProductsApiController(ProductionContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetProducts([FromQuery] string? category)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Category,
                prod_time = p.ProductionTimePerUnit,
                p.MinimalStock
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}/materials")]
    public async Task<ActionResult> GetProductMaterials(int id)
    {
        var exists = await _context.Products.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            return NotFound(new { error = "Продукт не найден." });
        }

        var materials = await _context.ProductMaterials
            .Include(pm => pm.Material)
            .Where(pm => pm.ProductId == id)
            .Select(pm => new
            {
                pm.MaterialId,
                name = pm.Material.Name,
                quantity_needed = pm.QuantityNeeded,
                unit = pm.Material.UnitOfMeasure,
                available = pm.Material.Quantity
            })
            .ToListAsync();

        return Ok(materials);
    }

    [HttpPost]
    public async Task<ActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "name обязателен." });
        }

        var product = new Models.Product
        {
            Name = request.Name.Trim(),
            Category = request.Category,
            ProductionTimePerUnit = request.ProdTime,
            Specifications = "{}"
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }
}
