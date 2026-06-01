using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Dtos;
using ProductionSystem.Services;

namespace ProductionSystem.Controllers.Api;

[ApiController]
[Route("api/calculate")]
public class CalculateApiController : ControllerBase
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public CalculateApiController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    [HttpPost("production")]
    public async Task<ActionResult<CalculationResponse>> CalculateProduction([FromBody] CalculateProductionRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product is null)
        {
            return NotFound(new { error = "Продукт не найден." });
        }

        var minutes = await _production.CalculateProductionMinutesAsync(
            request.ProductId,
            request.Quantity,
            request.LineId);

        return Ok(new CalculationResponse
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Minutes = minutes,
            Formula = $"({request.Quantity} × {product.ProductionTimePerUnit}) / коэфф. эффективности = {minutes} мин"
        });
    }
}
