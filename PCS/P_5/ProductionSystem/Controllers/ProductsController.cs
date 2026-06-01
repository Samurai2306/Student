using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;
using ProductionSystem.ViewModels;

namespace ProductionSystem.Controllers;

public class ProductsController : Controller
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public ProductsController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    public async Task<IActionResult> Index(string? category, string? search)
    {
        return View(await BuildIndexViewModel(category, search));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductMaterials)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var materials = await _context.Materials.OrderBy(m => m.Name).ToListAsync();
        var linked = product.ProductMaterials.ToDictionary(pm => pm.MaterialId, pm => pm.QuantityNeeded);

        var model = new ProductsIndexViewModel
        {
            Categories = await _context.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync(),
            Products = await _context.Products.OrderBy(p => p.Name).ToListAsync(),
            Form = new ProductFormModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Specifications = product.Specifications,
                Category = product.Category,
                MinimalStock = product.MinimalStock,
                ProductionTimePerUnit = product.ProductionTimePerUnit,
                Materials = materials.Select(m => new ProductMaterialInput
                {
                    MaterialId = m.Id,
                    MaterialName = m.Name,
                    Selected = linked.ContainsKey(m.Id),
                    QuantityNeeded = linked.TryGetValue(m.Id, out var qty) ? qty : 1
                }).ToList()
            }
        };

        ViewData["EditMode"] = true;
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormModel form)
    {
        if (!IsValidJson(form.Specifications))
        {
            ModelState.AddModelError(nameof(form.Specifications), "Specifications должны быть валидным JSON.");
        }

        if (!ModelState.IsValid)
        {
            var vm = await BuildIndexViewModel(null, null);
            vm.Form = form;
            return View("Index", vm);
        }

        var product = new Product
        {
            Name = form.Name.Trim(),
            Description = form.Description,
            Specifications = string.IsNullOrWhiteSpace(form.Specifications) ? "{}" : form.Specifications,
            Category = form.Category.Trim(),
            MinimalStock = form.MinimalStock,
            ProductionTimePerUnit = form.ProductionTimePerUnit
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        await SaveProductMaterialsAsync(product.Id, form.Materials);

        TempData["Message"] = "Продукт создан.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormModel form)
    {
        if (!form.Id.HasValue)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!IsValidJson(form.Specifications))
        {
            ModelState.AddModelError(nameof(form.Specifications), "Specifications должны быть валидным JSON.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["EditMode"] = true;
            var vm = await BuildIndexViewModel(null, null);
            vm.Form = form;
            return View("Index", vm);
        }

        try
        {
            var materials = form.Materials
                .Where(m => m.Selected && m.QuantityNeeded > 0)
                .Select(m => (m.MaterialId, m.QuantityNeeded));

            await _production.UpdateProductAsync(
                form.Id.Value,
                form.Name,
                form.Description,
                string.IsNullOrWhiteSpace(form.Specifications) ? "{}" : form.Specifications,
                form.Category,
                form.MinimalStock,
                form.ProductionTimePerUnit,
                materials);

            TempData["Message"] = "Продукт обновлён.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductsIndexViewModel> BuildIndexViewModel(string? category, string? search)
    {
        var query = _context.Products
            .Include(p => p.ProductMaterials)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var materials = await _context.Materials.OrderBy(m => m.Name).ToListAsync();

        return new ProductsIndexViewModel
        {
            Category = category,
            Search = search,
            Categories = await _context.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync(),
            Products = await query.OrderBy(p => p.Name).ToListAsync(),
            Form = new ProductFormModel
            {
                Materials = materials.Select(m => new ProductMaterialInput
                {
                    MaterialId = m.Id,
                    MaterialName = m.Name
                }).ToList()
            }
        };
    }

    private async Task SaveProductMaterialsAsync(int productId, List<ProductMaterialInput> items)
    {
        foreach (var item in items.Where(m => m.Selected && m.QuantityNeeded > 0))
        {
            _context.ProductMaterials.Add(new ProductMaterial
            {
                ProductId = productId,
                MaterialId = item.MaterialId,
                QuantityNeeded = item.QuantityNeeded
            });
        }

        await _context.SaveChangesAsync();
    }

    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
