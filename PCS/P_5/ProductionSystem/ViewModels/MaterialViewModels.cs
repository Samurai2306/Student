using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.ViewModels;

public sealed class MaterialFormModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Укажите название")]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Укажите единицу измерения")]
    public string UnitOfMeasure { get; set; } = "шт";

    [Range(0, double.MaxValue)]
    public decimal MinimalStock { get; set; }
}

public sealed class ReplenishMaterialModel
{
    public int MaterialId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Укажите положительное количество")]
    public decimal Amount { get; set; } = 10;
}

public sealed class MaterialsIndexViewModel
{
    public IReadOnlyList<Models.Material> Materials { get; init; } = [];
    public MaterialFormModel NewMaterial { get; set; } = new();
    public MaterialFormModel? EditMaterial { get; set; }
    public int LowStockCount => Materials.Count(m => m.IsLowStock);
}
