using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Dtos;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers.Api;

[ApiController]
[Route("api/lines")]
public class LinesApiController : ControllerBase
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public LinesApiController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    [HttpGet]
    public async Task<ActionResult> GetLines([FromQuery] bool available = false)
    {
        var query = _context.ProductionLines
            .Include(l => l.CurrentWorkOrder)
            .ThenInclude(o => o!.Product)
            .AsQueryable();

        if (available)
        {
            query = query.Where(l => l.Status == ProductionLine.StatusActive && l.CurrentWorkOrderId == null);
        }

        var items = await query
            .OrderBy(l => l.Name)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Status,
                efficiency = l.EfficiencyFactor,
                l.CurrentWorkOrderId,
                current_product = l.CurrentWorkOrder != null ? l.CurrentWorkOrder.Product.Name : null,
                progress = l.CurrentWorkOrder != null ? l.CurrentWorkOrder.ProgressPercent : 0
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] LineStatusRequest request)
    {
        try
        {
            await _production.UpdateLineStatusAsync(id, request.Status);
            return Ok(new { id, status = request.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult> GetSchedule(int id)
    {
        var lineExists = await _context.ProductionLines.AnyAsync(l => l.Id == id);
        if (!lineExists)
        {
            return NotFound(new { error = "Линия не найдена." });
        }

        var schedule = await _context.WorkOrders
            .Include(o => o.Product)
            .Where(o => o.ProductionLineId == id && o.Status != WorkOrder.StatusCancelled)
            .OrderBy(o => o.StartDate)
            .Select(o => new
            {
                o.Id,
                product = o.Product.Name,
                o.Quantity,
                o.StartDate,
                o.EstimatedEndDate,
                o.Status,
                o.ProgressPercent
            })
            .ToListAsync();

        return Ok(schedule);
    }
}
