namespace ProductionSystem.Models;

public class WorkOrder
{
    public const string StatusPending = "Pending";
    public const string StatusInProgress = "InProgress";
    public const string StatusCompleted = "Completed";
    public const string StatusCancelled = "Cancelled";

    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int? ProductionLineId { get; set; }

    public ProductionLine? ProductionLine { get; set; }

    public int Quantity { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EstimatedEndDate { get; set; }

    public string Status { get; set; } = StatusPending;

    public int ProgressPercent { get; set; }
}
