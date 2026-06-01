using System.ComponentModel.DataAnnotations;

namespace ProductionSystem.ViewModels;

public sealed class ProductMaterialInput
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public bool Selected { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal QuantityNeeded { get; set; } = 1;
}

public sealed class ProductFormModel
{
    public int? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Specifications { get; set; } = "{}";

    [Required]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int MinimalStock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Время производства должно быть больше 0")]
    public int ProductionTimePerUnit { get; set; } = 60;

    public List<ProductMaterialInput> Materials { get; set; } = [];
}

public sealed class ProductsIndexViewModel
{
    public string? Category { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<Models.Product> Products { get; init; } = [];
    public ProductFormModel Form { get; set; } = new();
}
