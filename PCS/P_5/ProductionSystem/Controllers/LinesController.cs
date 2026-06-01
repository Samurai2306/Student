using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;
using ProductionSystem.ViewModels;

namespace ProductionSystem.Controllers;

public class LinesController : Controller
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public LinesController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    public async Task<IActionResult> Index()
    {
        var model = new LinesIndexViewModel
        {
            Lines = await _context.ProductionLines
                .Include(l => l.CurrentWorkOrder)
                .ThenInclude(o => o!.Product)
                .Include(l => l.WorkOrders.Where(o => o.Status != WorkOrder.StatusCancelled))
                .ThenInclude(o => o.Product)
                .OrderBy(l => l.Name)
                .ToListAsync(),
            PendingOrders = await _context.WorkOrders
                .Include(o => o.Product)
                .Where(o => o.Status == WorkOrder.StatusPending)
                .OrderBy(o => o.StartDate)
                .ToListAsync(),
            Products = await _context.Products.OrderBy(p => p.Name).ToListAsync(),
            Message = TempData["Message"] as string,
            Error = TempData["Error"] as string
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int lineId, string status)
    {
        try
        {
            await _production.UpdateLineStatusAsync(lineId, status);
            TempData["Message"] = status == ProductionLine.StatusActive
                ? "Линия активирована."
                : "Линия остановлена. Текущий заказ возвращён в ожидание.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEfficiency(LineActionModel model)
    {
        try
        {
            await _production.UpdateLineEfficiencyAsync(model.LineId, model.EfficiencyFactor);
            TempData["Message"] = "Коэффициент сохранён. Сроки ожидающих заказов пересчитаны.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartOrder(int lineId, int orderId)
    {
        var order = await _context.WorkOrders.FindAsync(orderId);
        if (order is null)
        {
            TempData["Error"] = "Заказ не найден.";
            return RedirectToAction(nameof(Index));
        }

        if (order.ProductionLineId != lineId)
        {
            order.ProductionLineId = lineId;
            await _context.SaveChangesAsync();
        }

        try
        {
            await _production.StartWorkOrderAsync(orderId);
            TempData["Message"] = $"Заказ #{orderId} запущен на линии.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickStart(LineQuickStartModel model)
    {
        try
        {
            var order = await _production.CreateAndStartOnLineAsync(model.LineId, model.ProductId, model.Quantity);
            TempData["Message"] = $"На линии создан и запущен заказ #{order.Id}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopLine(int lineId)
    {
        try
        {
            await _production.UpdateLineStatusAsync(lineId, ProductionLine.StatusStopped);
            TempData["Message"] = "Линия остановлена.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(RescheduleOrderModel model)
    {
        try
        {
            await _production.RescheduleWorkOrderAsync(model.OrderId, model.NewStartDate);
            TempData["Message"] = "Срок заказа перенесён.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProgress(int orderId, int percent)
    {
        try
        {
            await _production.UpdateProgressAsync(orderId, percent);
            TempData["Message"] = "Прогресс обновлён.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
