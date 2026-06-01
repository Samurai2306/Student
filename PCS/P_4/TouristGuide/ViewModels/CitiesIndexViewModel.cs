using TouristGuide.Models;

namespace TouristGuide.ViewModels;

public sealed class CitiesIndexViewModel
{
    public string? Search { get; init; }

    public string? Region { get; init; }

    public string? Sort { get; init; }

    public IReadOnlyList<City> Cities { get; init; } = [];

    public IReadOnlyList<string> Regions { get; init; } = [];

    public int TotalInDatabase { get; init; }

    public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

    public bool HasRegion => !string.IsNullOrWhiteSpace(Region);

    public bool HasFilters => HasSearch || HasRegion;
}
