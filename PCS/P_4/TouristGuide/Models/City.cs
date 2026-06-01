namespace TouristGuide.Models;

public class City
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public int Population { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string History { get; set; } = string.Empty;

    public string CoatOfArmsUrl { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public ICollection<Attraction> Attractions { get; set; } = new List<Attraction>();
}
