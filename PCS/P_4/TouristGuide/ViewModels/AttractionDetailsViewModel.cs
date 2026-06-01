using TouristGuide.Models;

namespace TouristGuide.ViewModels;

public sealed class AttractionDetailsViewModel
{
    public Attraction Attraction { get; init; } = null!;

    public IReadOnlyList<Attraction> RelatedAttractions { get; init; } = [];
}
