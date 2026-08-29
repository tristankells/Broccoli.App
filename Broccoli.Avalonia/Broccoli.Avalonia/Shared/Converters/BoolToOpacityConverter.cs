using System.Globalization;
using Avalonia.Data.Converters;

namespace Broccoli.Avalonia.Shared.Converters;

/// <summary>Maps true to 1.0 and false to <see cref="InactiveOpacity"/> (0.5), to dim inactive filter chips.</summary>
public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public const double InactiveOpacity = 0.5;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1.0 : InactiveOpacity;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
