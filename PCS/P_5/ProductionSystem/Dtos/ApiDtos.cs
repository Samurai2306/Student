using System.Text.Json.Serialization;

namespace ProductionSystem.Dtos;

public sealed class CreateMaterialRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("min_stock")]
    public decimal MinStock { get; set; }
}

public sealed class StockChangeRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class CreateProductRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("prod_time")]
    public int ProdTime { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

public sealed class CreateOrderRequest
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("line_id")]
    public int? LineId { get; set; }
}

public sealed class ProgressRequest
{
    [JsonPropertyName("percent")]
    public int Percent { get; set; }
}

public sealed class LineStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed class CalculateProductionRequest
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("line_id")]
    public int? LineId { get; set; }
}

public sealed class CalculationResponse
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Minutes { get; set; }
    public string Formula { get; set; } = string.Empty;
}
