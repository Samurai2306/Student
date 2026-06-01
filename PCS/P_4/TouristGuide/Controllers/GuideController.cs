using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;
using TouristGuide.ViewModels;

namespace TouristGuide.Controllers;

public class GuideController : Controller
{
    private readonly TouristGuideContext _context;

    public GuideController(TouristGuideContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Map(int? cityId)
    {
        var model = new MapViewModel
        {
            CityId = cityId,
            Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync(),
            FocusCity = cityId.HasValue
                ? await _context.Cities.FirstOrDefaultAsync(c => c.Id == cityId.Value)
                : null
        };

        return View(model);
    }

    public IActionResult Planner()
    {
        return View();
    }
}
