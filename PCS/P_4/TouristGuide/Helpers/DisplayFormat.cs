using System.Globalization;

namespace TouristGuide.Helpers;

public static class DisplayFormat
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static string Population(int value) => $"{value.ToString("N0", Ru)} чел.";

    public static string EntryFee(decimal? value)
    {
        if (!value.HasValue)
        {
            return "Бесплатно";
        }

        return $"{value.Value.ToString("N0", Ru)} ₽";
    }

    public static string Pluralize(int count, string one, string few, string many)
    {
        var n = Math.Abs(count) % 100;
        var n1 = n % 10;

        if (n is > 10 and < 20)
        {
            return many;
        }

        return n1 switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many
        };
    }
}
