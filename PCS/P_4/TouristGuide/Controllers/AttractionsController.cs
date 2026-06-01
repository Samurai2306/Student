using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;
using TouristGuide.ViewModels;

namespace TouristGuide.Controllers;

public class AttractionsController : Controller
{
    private readonly TouristGuideContext _context;

    public AttractionsController(TouristGuideContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? cityId, string? fee, string? sort)
    {
        var normalizedSearch = search?.Trim();
        var feeFilter = string.IsNullOrWhiteSpace(fee) ? "all" : fee.Trim().ToLower();
        var totalInDatabase = await _context.Attractions.CountAsync();

        var query = _context.Attractions
            .Include(a => a.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var term = normalizedSearch.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(term) ||
                a.ShortDescription.ToLower().Contains(term) ||
                a.City.Name.ToLower().Contains(term));
        }

        if (cityId.HasValue)
        {
            query = query.Where(a => a.CityId == cityId.Value);
        }

        query = feeFilter switch
        {
            "free" => query.Where(a => a.EntryFee == null),
            "paid" => query.Where(a => a.EntryFee != null),
            _ => query
        };

        query = sort switch
        {
            "city" => query.OrderBy(a => a.City.Name).ThenBy(a => a.Name),
            "price-asc" => query.OrderBy(a => a.EntryFee ?? 0).ThenBy(a => a.Name),
            "price-desc" => query.OrderByDescending(a => a.EntryFee ?? 0).ThenBy(a => a.Name),
            _ => query.OrderBy(a => a.Name)
        };

        var model = new AttractionsIndexViewModel
        {
            Search = normalizedSearch,
            CityId = cityId,
            FeeFilter = feeFilter,
            Sort = sort ?? "name",
            Attractions = await query.ToListAsync(),
            Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync(),
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

        var attraction = await _context.Attractions
            .Include(a => a.City)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attraction is null)
        {
            return NotFound();
        }

        var related = await _context.Attractions
            .Where(a => a.CityId == attraction.CityId && a.Id != attraction.Id)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var model = new AttractionDetailsViewModel
        {
            Attraction = attraction,
            RelatedAttractions = related
        };

        return View(model);
    }
}
