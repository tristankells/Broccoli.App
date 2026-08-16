using System.Globalization;
using Avalonia.Data.Converters;

namespace Broccoli.Avalonia.Shared.Converters;

public class SafeIntConverter : IValueConverter
{
    public static readonly SafeIntConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && int.TryParse(s, NumberStyles.Any, culture, out int i))
        {
            return i;
        }

        return 0;
    }
}
