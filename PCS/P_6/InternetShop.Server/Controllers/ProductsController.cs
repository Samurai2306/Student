using InternetShop.Server.Data;
using InternetShop.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternetShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ShopDbContext _context;

    public ProductsController(ShopDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> Get()
    {
        return Ok(await _context.Products.OrderBy(p => p.Name).ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Post([FromBody] Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        product.Id = 0;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] Product product)
    {
        if (product.Id <= 0)
        {
            return BadRequest(new { error = "Укажите Id товара для изменения." });
        }

        var existing = await _context.Products.FindAsync(product.Id);
        if (existing is null)
        {
            return NotFound(new { error = "Товар не найден." });
        }

        existing.Name = product.Name.Trim();
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Stock = product.Stock;
        existing.Category = product.Category.Trim();

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Укажите id товара." });
        }

        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound(new { error = "Товар не найден." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
