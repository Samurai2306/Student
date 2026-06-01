namespace ProductionSystem.ViewModels;

public sealed class DashboardViewModel
{
    public int MaterialsCount { get; init; }
    public int LowStockCount { get; init; }
    public int ProductsCount { get; init; }
    public int ActiveOrdersCount { get; init; }
    public int AvailableLinesCount { get; init; }
    public int InProgressOrdersCount { get; init; }
    public IReadOnlyList<Models.WorkOrder> RecentOrders { get; init; } = [];
    public IReadOnlyList<Models.Material> LowStockMaterials { get; init; } = [];
}
