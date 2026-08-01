using Avalonia.Data.Converters;
using System.Globalization;

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
        if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out var d))
            return d;
        return 0d;
    }
}

public class SafeIntConverter : IValueConverter
{
    public static readonly SafeIntConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && int.TryParse(s, NumberStyles.Any, culture, out var i))
            return i;
        return 0;
    }
}
