using System.Globalization;
using System.Windows.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Converters;

/// <summary>
/// Преобразует элемент фильтра в строку: null → «Все», жанр → название, автор → полное имя.
/// </summary>
public class FilterDisplayConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "Все";
        if (value is Genre g) return g.Name;
        if (value is Author a) return a.FullName;
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
