namespace TouristGuide.Models;

public class Attraction
{
    public int Id { get; set; }

    public int CityId { get; set; }

    public City City { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string History { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string OpeningHours { get; set; } = string.Empty;

    public decimal? EntryFee { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
