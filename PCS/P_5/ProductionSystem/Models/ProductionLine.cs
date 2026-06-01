namespace ProductionSystem.Models;

public class ProductionLine
{
    public const string StatusActive = "Active";
    public const string StatusStopped = "Stopped";

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = StatusStopped;

    public float EfficiencyFactor { get; set; } = 1.0f;

    public int? CurrentWorkOrderId { get; set; }

    public WorkOrder? CurrentWorkOrder { get; set; }

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    public bool IsAvailable => Status == StatusActive && CurrentWorkOrderId is null;
}
