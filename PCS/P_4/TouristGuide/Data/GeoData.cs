using Microsoft.EntityFrameworkCore;

namespace TouristGuide.Data;

public static class GeoData
{
    private static readonly Dictionary<string, (double Lat, double Lng)> Cities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Москва"] = (55.7558, 37.6173),
        ["Санкт-Петербург"] = (59.9343, 30.3351),
        ["Казань"] = (55.7887, 49.1221),
        ["Сочи"] = (43.6028, 39.7342),
        ["Новосибирск"] = (55.0084, 82.9357)
    };

    private static readonly Dictionary<(string City, string Attraction), (double Lat, double Lng)> Attractions = new()
    {
        [("Москва", "Красная площадь")] = (55.7539, 37.6208),
        [("Москва", "Третьяковская галерея")] = (55.7414, 37.6208),
        [("Москва", "ВДНХ")] = (55.8260, 37.6376),
        [("Санкт-Петербург", "Эрмитаж")] = (59.9398, 30.3146),
        [("Санкт-Петербург", "Петропавловская крепость")] = (59.9500, 30.3167),
        [("Казань", "Казанский Кремль")] = (55.7986, 49.1064),
        [("Казань", "Улица Баумана")] = (55.7897, 49.1225),
        [("Сочи", "Олимпийский парк")] = (43.4050, 39.9550),
        [("Сочи", "Дендрарий")] = (43.5676, 39.8064),
        [("Новосибирск", "Театр оперы и балета")] = (55.0302, 82.9204),
        [("Новосибирск", "Метро «Сибирская»")] = (55.0423, 82.9474)
    };

    public static void ApplyToDatabase(TouristGuideContext context)
    {
        foreach (var city in context.Cities.Include(c => c.Attractions))
        {
            if (Cities.TryGetValue(city.Name, out var cityCoords))
            {
                city.Latitude = cityCoords.Lat;
                city.Longitude = cityCoords.Lng;
            }

            foreach (var attraction in city.Attractions)
            {
                if (Attractions.TryGetValue((city.Name, attraction.Name), out var point))
                {
                    attraction.Latitude = point.Lat;
                    attraction.Longitude = point.Lng;
                }
                else if (city.Latitude != 0)
                {
                    attraction.Latitude = city.Latitude + 0.01;
                    attraction.Longitude = city.Longitude + 0.01;
                }
            }
        }

        context.SaveChanges();
    }
}
