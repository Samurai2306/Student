using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionSystem.Data;
using ProductionSystem.Models;
using ProductionSystem.Services;
using ProductionSystem.ViewModels;

namespace ProductionSystem.Controllers;

public class MaterialsController : Controller
{
    private readonly ProductionContext _context;
    private readonly ProductionService _production;

    public MaterialsController(ProductionContext context, ProductionService production)
    {
        _context = context;
        _production = production;
    }

    public async Task<IActionResult> Index(int? editId)
    {
        var materials = await _context.Materials.OrderBy(m => m.Name).ToListAsync();
        MaterialFormModel? editMaterial = null;

        if (editId.HasValue)
        {
            var material = materials.FirstOrDefault(m => m.Id == editId.Value);
            if (material is not null)
            {
                editMaterial = new MaterialFormModel
                {
                    Id = material.Id,
                    Name = material.Name,
                    Quantity = material.Quantity,
                    UnitOfMeasure = material.UnitOfMeasure,
                    MinimalStock = material.MinimalStock
                };
            }
        }

        return View(new MaterialsIndexViewModel
        {
            Materials = materials,
            EditMaterial = editMaterial
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new MaterialsIndexViewModel
            {
                Materials = await _context.Materials.OrderBy(m => m.Name).ToListAsync(),
                NewMaterial = model
            });
        }

        _context.Materials.Add(new Material
        {
            Name = model.Name.Trim(),
            Quantity = model.Quantity,
            UnitOfMeasure = model.UnitOfMeasure.Trim(),
            MinimalStock = model.MinimalStock
        });

        await _context.SaveChangesAsync();
        TempData["Message"] = "Материал добавлен.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MaterialFormModel model)
    {
        if (!model.Id.HasValue || !ModelState.IsValid)
        {
            TempData["Error"] = "Проверьте данные материала.";
            return RedirectToAction(nameof(Index), new { editId = model.Id });
        }

        try
        {
            await _production.UpdateMaterialAsync(
                model.Id.Value,
                model.Name,
                model.Quantity,
                model.UnitOfMeasure,
                model.MinimalStock);
            TempData["Message"] = "Материал обновлён.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replenish(ReplenishMaterialModel model)
    {
        try
        {
            await _production.ReplenishMaterialAsync(model.MaterialId, model.Amount);
            TempData["Message"] = "Склад пополнен.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
