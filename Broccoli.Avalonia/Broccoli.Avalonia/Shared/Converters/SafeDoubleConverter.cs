using System.Globalization;
using Avalonia.Data.Converters;

namespace Broccoli.Avalonia.Shared.Converters;

public class SafeDoubleConverter : IValueConverter
{
    public static readonly SafeDoubleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out double d))
        {
            return d;
        }

        return 0d;
    }
}
