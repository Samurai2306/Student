using ProductionSystem.Models;

namespace ProductionSystem.Data;

public static class DbInitializer
{
    public static void Initialize(ProductionContext context)
    {
        context.Database.EnsureCreated();

        if (context.Products.Any())
        {
            return;
        }

        var steel = new Material { Name = "Стальной лист", Quantity = 120, UnitOfMeasure = "кг", MinimalStock = 40 };
        var bolts = new Material { Name = "Болты M8", Quantity = 500, UnitOfMeasure = "шт", MinimalStock = 200 };
        var paint = new Material { Name = "Порошковая краска", Quantity = 25, UnitOfMeasure = "кг", MinimalStock = 10 };
        var plastic = new Material { Name = "Пластик ABS", Quantity = 80, UnitOfMeasure = "кг", MinimalStock = 30 };
        var chips = new Material { Name = "Микросхемы", Quantity = 15, UnitOfMeasure = "шт", MinimalStock = 20 };

        context.Materials.AddRange(steel, bolts, paint, plastic, chips);

        var cabinet = new Product
        {
            Name = "Металлический шкаф",
            Description = "Шкаф для производственных помещений.",
            Specifications = """{"height_cm":180,"width_cm":90,"doors":2}""",
            Category = "Мебель",
            MinimalStock = 5,
            ProductionTimePerUnit = 120
        };

        var panel = new Product
        {
            Name = "Панель управления",
            Description = "Электронная панель для станков.",
            Specifications = """{"voltage":"220V","display":"LCD"}""",
            Category = "Электроника",
            MinimalStock = 3,
            ProductionTimePerUnit = 90
        };

        var bracket = new Product
        {
            Name = "Кронштейн универсальный",
            Description = "Крепёжный элемент для оборудования.",
            Specifications = """{"material":"steel","load_kg":25}""",
            Category = "Комплектующие",
            MinimalStock = 10,
            ProductionTimePerUnit = 45
        };

        context.Products.AddRange(cabinet, panel, bracket);
        context.SaveChanges();

        context.ProductMaterials.AddRange(
            new ProductMaterial { ProductId = cabinet.Id, MaterialId = steel.Id, QuantityNeeded = 8 },
            new ProductMaterial { ProductId = cabinet.Id, MaterialId = bolts.Id, QuantityNeeded = 24 },
            new ProductMaterial { ProductId = cabinet.Id, MaterialId = paint.Id, QuantityNeeded = 1.5m },
            new ProductMaterial { ProductId = panel.Id, MaterialId = plastic.Id, QuantityNeeded = 2 },
            new ProductMaterial { ProductId = panel.Id, MaterialId = chips.Id, QuantityNeeded = 3 },
            new ProductMaterial { ProductId = bracket.Id, MaterialId = steel.Id, QuantityNeeded = 1.5m },
            new ProductMaterial { ProductId = bracket.Id, MaterialId = bolts.Id, QuantityNeeded = 4 });

        var lineA = new ProductionLine { Name = "Линия A — сборка", Status = ProductionLine.StatusActive, EfficiencyFactor = 1.0f };
        var lineB = new ProductionLine { Name = "Линия B — покраска", Status = ProductionLine.StatusActive, EfficiencyFactor = 1.2f };
        var lineC = new ProductionLine { Name = "Линия C — электроника", Status = ProductionLine.StatusStopped, EfficiencyFactor = 0.9f };

        context.ProductionLines.AddRange(lineA, lineB, lineC);
        context.SaveChanges();

        var order1 = new WorkOrder
        {
            ProductId = bracket.Id,
            ProductionLineId = lineA.Id,
            Quantity = 5,
            StartDate = DateTime.Today.AddHours(9),
            EstimatedEndDate = DateTime.Today.AddHours(9).AddMinutes(225),
            Status = WorkOrder.StatusPending,
            ProgressPercent = 0
        };

        var order2 = new WorkOrder
        {
            ProductId = cabinet.Id,
            ProductionLineId = lineB.Id,
            Quantity = 2,
            StartDate = DateTime.Today.AddDays(1).AddHours(10),
            EstimatedEndDate = DateTime.Today.AddDays(1).AddHours(14),
            Status = WorkOrder.StatusPending,
            ProgressPercent = 0
        };

        context.WorkOrders.AddRange(order1, order2);
        context.SaveChanges();
    }
}
