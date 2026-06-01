using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Dtos;
using ProductionSystem.Models;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers.Api;

[ApiController]
[Route("api/orders")]
public class OrdersApiController : ControllerBase
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public OrdersApiController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    [HttpGet]
    public async Task<ActionResult> GetOrders([FromQuery] string? status, [FromQuery] string? date)
    {
        var query = _context.WorkOrders
            .Include(o => o.Product)
            .Include(o => o.ProductionLine)
            .AsQueryable();

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o =>
                o.Status == WorkOrder.StatusPending || o.Status == WorkOrder.StatusInProgress);
        }

        if (string.Equals(date, "today", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.Today;
            query = query.Where(o => o.StartDate.Date == today || o.EstimatedEndDate.Date == today);
        }

        var items = await query
            .OrderByDescending(o => o.StartDate)
            .Select(o => new
            {
                o.Id,
                product = o.Product.Name,
                o.Quantity,
                o.Status,
                o.StartDate,
                o.EstimatedEndDate,
                line = o.ProductionLine != null ? o.ProductionLine.Name : null,
                o.ProgressPercent
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("progress")]
    public async Task<ActionResult> GetProgress()
    {
        await _production.SyncInProgressOrdersAsync();

        var items = await _context.WorkOrders
            .Where(o => o.Status == WorkOrder.StatusInProgress)
            .Select(o => new
            {
                o.Id,
                percent = o.ProgressPercent,
                o.Status,
                o.EstimatedEndDate,
                o.StartDate
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var order = await _production.CreateWorkOrderAsync(request.ProductId, request.Quantity, request.LineId);
            return Ok(new
            {
                order.Id,
                order.ProductId,
                order.Quantity,
                order.StartDate,
                order.EstimatedEndDate,
                order.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}/progress")]
    public async Task<ActionResult> UpdateProgress(int id, [FromBody] ProgressRequest request)
    {
        try
        {
            await _production.UpdateProgressAsync(id, request.Percent);
            var order = await _context.WorkOrders.FindAsync(id);
            return Ok(new { order!.Id, order.ProgressPercent, order.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult> GetDetails(int id)
    {
        var order = await _context.WorkOrders
            .Include(o => o.Product)
            .ThenInclude(p => p.ProductMaterials)
            .ThenInclude(pm => pm.Material)
            .Include(o => o.ProductionLine)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(new { error = "Заказ не найден." });
        }

        return Ok(new
        {
            order.Id,
            product = new
            {
                order.Product.Id,
                order.Product.Name,
                order.Product.Category,
                order.Product.ProductionTimePerUnit
            },
            order.Quantity,
            order.Status,
            order.ProgressPercent,
            order.StartDate,
            order.EstimatedEndDate,
            line = order.ProductionLine != null
                ? new { order.ProductionLine.Id, order.ProductionLine.Name, order.ProductionLine.EfficiencyFactor }
                : null,
            materials = order.Product.ProductMaterials.Select(pm => new
            {
                pm.Material.Name,
                needed = pm.QuantityNeeded * order.Quantity,
                unit = pm.Material.UnitOfMeasure,
                available = pm.Material.Quantity
            })
        });
    }
}
