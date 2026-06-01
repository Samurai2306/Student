using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;
using ProductionSystem.ViewModels;

namespace ProductionSystem.Controllers;

public class OrdersController : Controller
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public OrdersController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    public async Task<IActionResult> Index(string? status)
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
        else if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var model = new OrdersIndexViewModel
        {
            StatusFilter = status,
            Orders = await query.OrderByDescending(o => o.StartDate).ToListAsync(),
            Products = await _context.Products.OrderBy(p => p.Name).ToListAsync(),
            AvailableLines = await _context.ProductionLines
                .Where(l => l.Status == ProductionLine.StatusActive && l.CurrentWorkOrderId == null)
                .OrderBy(l => l.Name)
                .ToListAsync(),
            Message = TempData["Message"] as string,
            Error = TempData["Error"] as string
        };

        if (model.Products.Count > 0)
        {
            model.NewOrder.ProductId = model.Products[0].Id;
        }

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.WorkOrders
            .Include(o => o.Product)
            .ThenInclude(p => p.ProductMaterials)
            .ThenInclude(pm => pm.Material)
            .Include(o => o.ProductionLine)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var minutes = await _production.CalculateProductionMinutesAsync(
            order.ProductId,
            order.Quantity,
            order.ProductionLineId);

        var model = new OrderDetailsViewModel
        {
            Order = order,
            ProductionMinutes = minutes,
            MaterialPlan = order.Product.ProductMaterials
                .Select(pm => (
                    pm.Material.Name,
                    pm.QuantityNeeded * order.Quantity,
                    pm.Material.Quantity,
                    pm.Material.UnitOfMeasure))
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormModel model)
    {
        try
        {
            await _production.CreateWorkOrderAsync(model.ProductId, model.Quantity, model.ProductionLineId, model.StartDate);
            TempData["Message"] = "Заказ создан. Материалы проверены.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            await _production.StartWorkOrderAsync(id);
            TempData["Message"] = $"Заказ #{id} запущен в производство.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await _production.CancelWorkOrderAsync(id);
            TempData["Message"] = $"Заказ #{id} отменён. Материалы возвращены, если заказ был в работе.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Calculate(int productId, int quantity, int? lineId, DateTime? startDate)
    {
        try
        {
            var minutes = await _production.CalculateProductionMinutesAsync(productId, quantity, lineId);
            var start = startDate ?? DateTime.Now;
            var materialErrors = await _production.ValidateMaterialsAsync(productId, quantity);

            return Json(new
            {
                minutes,
                endDate = start.AddMinutes(minutes),
                materialsOk = materialErrors.Count == 0,
                materialMessages = materialErrors
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
