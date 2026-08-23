using System.Globalization;
using Avalonia.Data.Converters;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Shared.Converters;

/// <summary>Maps a <see cref="SeasonalityState"/> to its display colour (green = in, orange = partial, gray = out).</summary>
public class SeasonalityStateToColorConverter : IValueConverter
{
    public static readonly SeasonalityStateToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is SeasonalityState state ? StateColor(state) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static string StateColor(SeasonalityState state) => state switch
    {
        SeasonalityState.InSeason => "#2ECC71",
        SeasonalityState.PartiallyInSeason => "#F39C12",
        _ => "Gray",
    };
}

/// <summary>Maps a <see cref="SeasonalityState"/> to a short display label for dropdown items.</summary>
public class SeasonalityStateToLabelConverter : IValueConverter
{
    public static readonly SeasonalityStateToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is SeasonalityState state ? StateLabel(state) : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static string StateLabel(SeasonalityState state) => state switch
    {
        SeasonalityState.InSeason => "In season",
        SeasonalityState.PartiallyInSeason => "Partial",
        _ => "Out",
    };
}
