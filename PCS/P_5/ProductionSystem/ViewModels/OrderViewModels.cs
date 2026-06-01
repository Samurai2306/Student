using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.ViewModels;

public sealed class OrderFormModel
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public int? ProductionLineId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Now;

    public int? CalculatedMinutes { get; set; }

    public DateTime? CalculatedEndDate { get; set; }
}

public sealed class OrdersIndexViewModel
{
    public string? StatusFilter { get; init; }
    public IReadOnlyList<Models.WorkOrder> Orders { get; init; } = [];
    public IReadOnlyList<Models.Product> Products { get; init; } = [];
    public IReadOnlyList<Models.ProductionLine> AvailableLines { get; init; } = [];
    public OrderFormModel NewOrder { get; set; } = new();
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public sealed class OrderDetailsViewModel
{
    public Models.WorkOrder Order { get; init; } = null!;
    public IReadOnlyList<(string Name, decimal Needed, decimal Available, string Unit)> MaterialPlan { get; init; } = [];
    public int ProductionMinutes { get; init; }
}

public sealed class RescheduleOrderModel
{
    public int OrderId { get; set; }

    [Required]
    public DateTime NewStartDate { get; set; } = DateTime.Now;
}

public sealed class LineActionModel
{
    public int LineId { get; set; }
    public float EfficiencyFactor { get; set; } = 1.0f;
    public int? PendingOrderId { get; set; }
}

public sealed class LineQuickStartModel
{
    public int LineId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}

public sealed class LinesIndexViewModel
{
    public IReadOnlyList<Models.ProductionLine> Lines { get; init; } = [];
    public IReadOnlyList<Models.WorkOrder> PendingOrders { get; init; } = [];
    public IReadOnlyList<Models.Product> Products { get; init; } = [];
    public string? Message { get; set; }
    public string? Error { get; set; }
}
