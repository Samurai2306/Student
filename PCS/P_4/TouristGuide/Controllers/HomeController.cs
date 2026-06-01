using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;
using TouristGuide.ViewModels;

namespace TouristGuide.Controllers;

public class HomeController : Controller
{
    private readonly TouristGuideContext _context;

    public HomeController(TouristGuideContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cities = await _context.Cities
            .Include(c => c.Attractions)
            .OrderByDescending(c => c.Population)
            .ToListAsync();

        var attractions = await _context.Attractions
            .Include(a => a.City)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var model = new DashboardViewModel
        {
            CitiesCount = cities.Count,
            AttractionsCount = attractions.Count,
            RegionsCount = cities.Select(c => c.Region).Distinct().Count(),
            FreeAttractionsCount = attractions.Count(a => !a.EntryFee.HasValue),
            TopCities = cities.Take(3).ToList(),
            FeaturedAttractions = attractions.Take(6).ToList()
        };

        return View(model);
    }

    public IActionResult Error()
    {
        return View();
    }
}
