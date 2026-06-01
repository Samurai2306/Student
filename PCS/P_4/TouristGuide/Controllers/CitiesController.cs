using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;
using TouristGuide.ViewModels;

namespace TouristGuide.Controllers;

public class CitiesController : Controller
{
    private readonly TouristGuideContext _context;

    public CitiesController(TouristGuideContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? region, string? sort)
    {
        var normalizedSearch = search?.Trim();
        var normalizedRegion = region?.Trim();
        var totalInDatabase = await _context.Cities.CountAsync();

        var regions = await _context.Cities
            .Select(c => c.Region)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();

        var query = _context.Cities
            .Include(c => c.Attractions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var term = normalizedSearch.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Region.ToLower().Contains(term) ||
                c.ShortDescription.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(normalizedRegion))
        {
            query = query.Where(c => c.Region == normalizedRegion);
        }

        query = sort switch
        {
            "population-desc" => query.OrderByDescending(c => c.Population),
            "population-asc" => query.OrderBy(c => c.Population),
            _ => query.OrderBy(c => c.Name)
        };

        var cities = await query.ToListAsync();

        var model = new CitiesIndexViewModel
        {
            Search = normalizedSearch,
            Region = normalizedRegion,
            Sort = sort ?? "name",
            Regions = regions,
            Cities = cities,
            TotalInDatabase = totalInDatabase
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var city = await _context.Cities
            .Include(c => c.Attractions.OrderBy(a => a.Name))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (city is null)
        {
            return NotFound();
        }

        return View(city);
    }
}
