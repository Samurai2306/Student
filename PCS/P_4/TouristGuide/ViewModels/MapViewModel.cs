using TouristGuide.Models;

namespace TouristGuide.ViewModels;

public sealed class MapViewModel
{
    public int? CityId { get; init; }

    public City? FocusCity { get; init; }

    public IReadOnlyList<City> Cities { get; init; } = [];
}
