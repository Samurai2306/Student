using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;

namespace ProductionSystem.Services;

public sealed class ProductionService
{
    private readonly ProductionContext _context;

    public ProductionService(ProductionContext context)
    {
        _context = context;
    }

    public async Task<int> CalculateProductionMinutesAsync(int productId, int quantity, int? lineId = null)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new InvalidOperationException("Продукт не найден.");

        var efficiency = 1.0f;
        if (lineId.HasValue)
        {
            var line = await _context.ProductionLines.FindAsync(lineId.Value)
                ?? throw new InvalidOperationException("Линия не найдена.");
            efficiency = Math.Clamp(line.EfficiencyFactor, 0.5f, 2.0f);
        }

        return (int)Math.Ceiling(product.ProductionTimePerUnit * quantity / efficiency);
    }

    public async Task<IReadOnlyList<string>> ValidateMaterialsAsync(int productId, int quantity)
    {
        var requirements = await _context.ProductMaterials
            .Include(pm => pm.Material)
            .Where(pm => pm.ProductId == productId)
            .ToListAsync();

        if (requirements.Count == 0)
        {
            return ["У продукта не задана рецептура материалов."];
        }

        var errors = new List<string>();
        foreach (var requirement in requirements)
        {
            var needed = requirement.QuantityNeeded * quantity;
            if (requirement.Material.Quantity < needed)
            {
                errors.Add(
                    $"Недостаточно «{requirement.Material.Name}»: нужно {needed} {requirement.Material.UnitOfMeasure}, " +
                    $"на складе {requirement.Material.Quantity} {requirement.Material.UnitOfMeasure}.");
            }
        }

        return errors;
    }

    public async Task<WorkOrder> CreateWorkOrderAsync(int productId, int quantity, int? lineId, DateTime? startDate = null)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Количество должно быть больше нуля.");
        }

        _ = await _context.Products.FindAsync(productId)
            ?? throw new InvalidOperationException("Продукт не найден.");

        if (lineId.HasValue)
        {
            var line = await _context.ProductionLines.FindAsync(lineId.Value)
                ?? throw new InvalidOperationException("Линия не найдена.");

            if (!line.IsAvailable)
            {
                throw new InvalidOperationException("Выбранная линия недоступна.");
            }
        }

        var materialErrors = await ValidateMaterialsAsync(productId, quantity);
        if (materialErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(' ', materialErrors));
        }

        var start = startDate ?? DateTime.Now;
        var minutes = await CalculateProductionMinutesAsync(productId, quantity, lineId);

        var order = new WorkOrder
        {
            ProductId = productId,
            ProductionLineId = lineId,
            Quantity = quantity,
            StartDate = start,
            EstimatedEndDate = start.AddMinutes(minutes),
            Status = WorkOrder.StatusPending,
            ProgressPercent = 0
        };

        _context.WorkOrders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<WorkOrder> CreateAndStartOnLineAsync(int lineId, int productId, int quantity)
    {
        var order = await CreateWorkOrderAsync(productId, quantity, lineId);
        await StartWorkOrderAsync(order.Id);
        return order;
    }

    public async Task StartWorkOrderAsync(int orderId)
    {
        var order = await _context.WorkOrders
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("Заказ не найден.");

        if (order.Status is WorkOrder.StatusCompleted or WorkOrder.StatusCancelled)
        {
            throw new InvalidOperationException("Нельзя запустить завершённый или отменённый заказ.");
        }

        var materialErrors = await ValidateMaterialsAsync(order.ProductId, order.Quantity);
        if (materialErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(' ', materialErrors));
        }

        if (order.ProductionLineId is null)
        {
            throw new InvalidOperationException("Для запуска нужно назначить производственную линию.");
        }

        var line = await _context.ProductionLines.FindAsync(order.ProductionLineId.Value)
            ?? throw new InvalidOperationException("Линия не найдена.");

        if (line.Status != ProductionLine.StatusActive)
        {
            throw new InvalidOperationException("Линия остановлена. Сначала активируйте линию.");
        }

        if (line.CurrentWorkOrderId is not null && line.CurrentWorkOrderId != order.Id)
        {
            throw new InvalidOperationException("На линии уже выполняется другой заказ.");
        }

        await DeductMaterialsAsync(order.ProductId, order.Quantity);

        order.Status = WorkOrder.StatusInProgress;
        order.ProgressPercent = Math.Max(order.ProgressPercent, 1);
        line.CurrentWorkOrderId = order.Id;

        await _context.SaveChangesAsync();
    }

    public async Task CancelWorkOrderAsync(int orderId)
    {
        var order = await _context.WorkOrders
            .Include(o => o.ProductionLine)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("Заказ не найден.");

        if (order.Status == WorkOrder.StatusCompleted)
        {
            throw new InvalidOperationException("Завершённый заказ нельзя отменить.");
        }

        if (order.Status == WorkOrder.StatusInProgress)
        {
            await ReturnMaterialsAsync(order.ProductId, order.Quantity);
        }

        if (order.ProductionLine is not null && order.ProductionLine.CurrentWorkOrderId == order.Id)
        {
            order.ProductionLine.CurrentWorkOrderId = null;
        }

        order.Status = WorkOrder.StatusCancelled;
        order.ProgressPercent = 0;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateProgressAsync(int orderId, int percent)
    {
        percent = Math.Clamp(percent, 0, 100);

        var order = await _context.WorkOrders
            .Include(o => o.ProductionLine)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("Заказ не найден.");

        if (order.Status == WorkOrder.StatusCancelled)
        {
            throw new InvalidOperationException("Нельзя изменить прогресс отменённого заказа.");
        }

        order.ProgressPercent = percent;

        if (percent >= 100)
        {
            order.Status = WorkOrder.StatusCompleted;
            order.ProgressPercent = 100;

            if (order.ProductionLine is not null && order.ProductionLine.CurrentWorkOrderId == order.Id)
            {
                order.ProductionLine.CurrentWorkOrderId = null;
            }
        }
        else if (order.Status == WorkOrder.StatusPending && percent > 0)
        {
            order.Status = WorkOrder.StatusInProgress;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RescheduleWorkOrderAsync(int orderId, DateTime newStartDate)
    {
        var order = await _context.WorkOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Заказ не найден.");

        if (order.Status == WorkOrder.StatusCancelled)
        {
            throw new InvalidOperationException("Нельзя перенести отменённый заказ.");
        }

        var minutes = await CalculateProductionMinutesAsync(order.ProductId, order.Quantity, order.ProductionLineId);
        order.StartDate = newStartDate;
        order.EstimatedEndDate = newStartDate.AddMinutes(minutes);
        await _context.SaveChangesAsync();
    }

    public async Task ReplenishMaterialAsync(int materialId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Количество пополнения должно быть больше нуля.");
        }

        var material = await _context.Materials.FindAsync(materialId)
            ?? throw new InvalidOperationException("Материал не найден.");

        material.Quantity += amount;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMaterialAsync(int id, string name, decimal quantity, string unit, decimal minimalStock)
    {
        var material = await _context.Materials.FindAsync(id)
            ?? throw new InvalidOperationException("Материал не найден.");

        material.Name = name.Trim();
        material.Quantity = quantity;
        material.UnitOfMeasure = unit.Trim();
        material.MinimalStock = minimalStock;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateProductAsync(
        int id,
        string name,
        string description,
        string specifications,
        string category,
        int minimalStock,
        int productionTimePerUnit,
        IEnumerable<(int MaterialId, decimal QuantityNeeded)> materials)
    {
        var product = await _context.Products
            .Include(p => p.ProductMaterials)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Продукт не найден.");

        product.Name = name.Trim();
        product.Description = description;
        product.Specifications = specifications;
        product.Category = category.Trim();
        product.MinimalStock = minimalStock;
        product.ProductionTimePerUnit = productionTimePerUnit;

        _context.ProductMaterials.RemoveRange(product.ProductMaterials);
        foreach (var (materialId, quantityNeeded) in materials)
        {
            _context.ProductMaterials.Add(new ProductMaterial
            {
                ProductId = id,
                MaterialId = materialId,
                QuantityNeeded = quantityNeeded
            });
        }

        await _context.SaveChangesAsync();
        await RecalculatePendingOrdersForProductAsync(id);
    }

    public async Task UpdateLineStatusAsync(int lineId, string status)
    {
        if (status is not (ProductionLine.StatusActive or ProductionLine.StatusStopped))
        {
            throw new InvalidOperationException("Статус должен быть Active или Stopped.");
        }

        var line = await _context.ProductionLines
            .Include(l => l.CurrentWorkOrder)
            .FirstOrDefaultAsync(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Линия не найдена.");

        if (status == ProductionLine.StatusStopped && line.CurrentWorkOrder is not null)
        {
            await ReturnMaterialsAsync(line.CurrentWorkOrder.ProductId, line.CurrentWorkOrder.Quantity);
            line.CurrentWorkOrder.Status = WorkOrder.StatusPending;
            line.CurrentWorkOrder.ProgressPercent = 0;
            line.CurrentWorkOrderId = null;
        }

        line.Status = status;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateLineEfficiencyAsync(int lineId, float factor)
    {
        var line = await _context.ProductionLines.FindAsync(lineId)
            ?? throw new InvalidOperationException("Линия не найдена.");

        line.EfficiencyFactor = Math.Clamp(factor, 0.5f, 2.0f);
        await _context.SaveChangesAsync();
        await RecalculatePendingOrdersForLineAsync(lineId);
    }

    private async Task RecalculatePendingOrdersForLineAsync(int lineId)
    {
        var orders = await _context.WorkOrders
            .Where(o => o.ProductionLineId == lineId && o.Status == WorkOrder.StatusPending)
            .ToListAsync();

        foreach (var order in orders)
        {
            var minutes = await CalculateProductionMinutesAsync(order.ProductId, order.Quantity, lineId);
            order.EstimatedEndDate = order.StartDate.AddMinutes(minutes);
        }

        await _context.SaveChangesAsync();
    }

    private async Task RecalculatePendingOrdersForProductAsync(int productId)
    {
        var orders = await _context.WorkOrders
            .Where(o => o.ProductId == productId && o.Status == WorkOrder.StatusPending)
            .ToListAsync();

        foreach (var order in orders)
        {
            var minutes = await CalculateProductionMinutesAsync(order.ProductId, order.Quantity, order.ProductionLineId);
            order.EstimatedEndDate = order.StartDate.AddMinutes(minutes);
        }

        await _context.SaveChangesAsync();
    }

    private async Task DeductMaterialsAsync(int productId, int quantity)
    {
        var requirements = await _context.ProductMaterials
            .Include(pm => pm.Material)
            .Where(pm => pm.ProductId == productId)
            .ToListAsync();

        foreach (var requirement in requirements)
        {
            requirement.Material.Quantity -= requirement.QuantityNeeded * quantity;
        }
    }

    private async Task ReturnMaterialsAsync(int productId, int quantity)
    {
        var requirements = await _context.ProductMaterials
            .Include(pm => pm.Material)
            .Where(pm => pm.ProductId == productId)
            .ToListAsync();

        foreach (var requirement in requirements)
        {
            requirement.Material.Quantity += requirement.QuantityNeeded * quantity;
        }

        await Task.CompletedTask;
    }
}

