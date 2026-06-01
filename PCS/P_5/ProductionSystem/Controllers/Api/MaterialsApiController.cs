using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Dtos;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers.Api;

[ApiController]
[Route("api/materials")]
public class MaterialsApiController : ControllerBase
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public MaterialsApiController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetMaterials([FromQuery] bool low_stock = false)
    {
        var query = _context.Materials.AsQueryable();
        if (low_stock)
        {
            query = query.Where(m => m.Quantity <= m.MinimalStock);
        }

        var items = await query
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Quantity,
                unit = m.UnitOfMeasure,
                min_stock = m.MinimalStock,
                low_stock = m.Quantity <= m.MinimalStock
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "name обязателен." });
        }

        var material = new Material
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitOfMeasure = request.Unit,
            MinimalStock = request.MinStock
        };

        _context.Materials.Add(material);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMaterials), new { id = material.Id }, material);
    }

    [HttpPut("{id:int}/stock")]
    public async Task<ActionResult> UpdateStock(int id, [FromBody] StockChangeRequest request)
    {
        try
        {
            await _production.ReplenishMaterialAsync(id, request.Amount);
            var material = await _context.Materials.FindAsync(id);
            return Ok(new { material!.Id, material.Quantity });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
