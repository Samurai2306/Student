using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.ViewModels;

namespace ProductionSystem.Controllers;

public class HomeController : Controller
{
    private readonly ProductionContext _context;

    public HomeController(ProductionContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var materials = await _context.Materials.ToListAsync();
        var model = new DashboardViewModel
        {
            MaterialsCount = materials.Count,
            LowStockCount = materials.Count(m => m.IsLowStock),
            ProductsCount = await _context.Products.CountAsync(),
            ActiveOrdersCount = await _context.WorkOrders.CountAsync(o =>
                o.Status == WorkOrder.StatusPending || o.Status == WorkOrder.StatusInProgress),
            InProgressOrdersCount = await _context.WorkOrders.CountAsync(o => o.Status == WorkOrder.StatusInProgress),
            AvailableLinesCount = await _context.ProductionLines.CountAsync(l =>
                l.Status == ProductionLine.StatusActive && l.CurrentWorkOrderId == null),
            LowStockMaterials = materials.Where(m => m.IsLowStock).OrderBy(m => m.Name).ToList(),
            RecentOrders = await _context.WorkOrders
                .Include(o => o.Product)
                .Include(o => o.ProductionLine)
                .OrderByDescending(o => o.StartDate)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult ApiDocs()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
