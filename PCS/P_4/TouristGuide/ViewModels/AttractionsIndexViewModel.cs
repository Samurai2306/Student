using TouristGuide.Models;

namespace TouristGuide.ViewModels;

public sealed class AttractionsIndexViewModel
{
    public string? Search { get; init; }

    public int? CityId { get; init; }

    public string? FeeFilter { get; init; }

    public string? Sort { get; init; }

    public IReadOnlyList<Attraction> Attractions { get; init; } = [];

    public IReadOnlyList<City> Cities { get; init; } = [];

    public int TotalInDatabase { get; init; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        CityId.HasValue ||
        !string.IsNullOrWhiteSpace(FeeFilter) && FeeFilter != "all";
}
