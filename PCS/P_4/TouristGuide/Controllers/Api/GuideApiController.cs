using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;
using TouristGuide.Helpers;

namespace TouristGuide.Controllers.Api;

[ApiController]
[Route("api/guide")]
public class GuideApiController : ControllerBase
{
    private readonly TouristGuideContext _context;

    public GuideApiController(TouristGuideContext context)
    {
        _context = context;
    }

    [HttpGet("search")]
    public async Task<ActionResult> Search([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Ok(new { cities = Array.Empty<object>(), attractions = Array.Empty<object>() });
        }

        var term = q.Trim().ToLower();

        var cities = await _context.Cities
            .Where(c => c.Name.ToLower().Contains(term) || c.Region.ToLower().Contains(term))
            .OrderBy(c => c.Name)
            .Take(5)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Region,
                url = Url.Action("Details", "Cities", new { id = c.Id })!
            })
            .ToListAsync();

        var attractions = await _context.Attractions
            .Include(a => a.City)
            .Where(a =>
                a.Name.ToLower().Contains(term) ||
                a.ShortDescription.ToLower().Contains(term) ||
                a.City.Name.ToLower().Contains(term))
            .OrderBy(a => a.Name)
            .Take(8)
            .Select(a => new
            {
                a.Id,
                a.Name,
                city = a.City.Name,
                fee = a.EntryFee,
                url = Url.Action("Details", "Attractions", new { id = a.Id })!
            })
            .ToListAsync();

        return Ok(new { cities, attractions });
    }

    [HttpGet("markers")]
    public async Task<ActionResult> Markers([FromQuery] int? cityId)
    {
        var citiesQuery = _context.Cities.AsQueryable();
        if (cityId.HasValue)
        {
            citiesQuery = citiesQuery.Where(c => c.Id == cityId.Value);
        }

        var cities = await citiesQuery
            .Select(c => new
            {
                type = "city",
                c.Id,
                c.Name,
                c.Latitude,
                c.Longitude,
                c.Region,
                url = Url.Action("Details", "Cities", new { id = c.Id })!
            })
            .Where(c => c.Latitude != 0)
            .ToListAsync();

        var attractionsQuery = _context.Attractions.Include(a => a.City).AsQueryable();
        if (cityId.HasValue)
        {
            attractionsQuery = attractionsQuery.Where(a => a.CityId == cityId.Value);
        }

        var attractionRows = await attractionsQuery
            .Where(a => a.Latitude != 0)
            .ToListAsync();

        var attractions = attractionRows.Select(a => new
        {
            type = "attraction",
            a.Id,
            a.Name,
            city = a.City.Name,
            a.Latitude,
            a.Longitude,
            feeText = DisplayFormat.EntryFee(a.EntryFee),
            a.EntryFee,
            url = Url.Action("Details", "Attractions", new { id = a.Id })!
        });

        return Ok(new { cities, attractions });
    }

    [HttpGet("random")]
    public async Task<ActionResult> Random()
    {
        var count = await _context.Attractions.CountAsync();
        if (count == 0)
        {
            return NotFound();
        }

        var index = System.Random.Shared.Next(count);
        var attraction = await _context.Attractions
            .Include(a => a.City)
            .OrderBy(a => a.Id)
            .Skip(index)
            .FirstAsync();

        return Ok(new
        {
            attraction.Id,
            attraction.Name,
            attraction.ShortDescription,
            city = attraction.City.Name,
            cityId = attraction.CityId,
            feeText = DisplayFormat.EntryFee(attraction.EntryFee),
            imageUrl = attraction.ImageUrl,
            url = Url.Action("Details", "Attractions", new { id = attraction.Id })!
        });
    }

    [HttpGet("catalog")]
    public async Task<ActionResult> Catalog()
    {
        var rows = await _context.Attractions
            .Include(a => a.City)
            .OrderBy(a => a.City.Name)
            .ThenBy(a => a.Name)
            .ToListAsync();

        var items = rows.Select(a => new
        {
            a.Id,
            a.Name,
            city = a.City.Name,
            cityId = a.CityId,
            a.EntryFee,
            feeText = DisplayFormat.EntryFee(a.EntryFee),
            imageUrl = a.ImageUrl,
            url = Url.Action("Details", "Attractions", new { id = a.Id })!
        });

        return Ok(items);
    }
}
