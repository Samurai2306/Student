using TouristGuide.Models;

namespace TouristGuide.ViewModels;

public sealed class DashboardViewModel
{
    public int CitiesCount { get; init; }

    public int AttractionsCount { get; init; }

    public int RegionsCount { get; init; }

    public int FreeAttractionsCount { get; init; }

    public IReadOnlyList<City> TopCities { get; init; } = [];

    public IReadOnlyList<Attraction> FeaturedAttractions { get; init; } = [];
}
