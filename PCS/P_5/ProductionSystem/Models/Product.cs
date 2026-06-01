namespace ProductionSystem.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Specifications { get; set; } = "{}";

    public string Category { get; set; } = string.Empty;

    public int MinimalStock { get; set; }

    public int ProductionTimePerUnit { get; set; }

    public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
