using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Broccoli.Avalonia.Shared.Converters;

/// <summary>
/// Shared "does the bound int equal the converter parameter" check used by the two highlight
/// converters below, so the column for the selected month can be tinted.
/// </summary>
internal static class MonthColumnMatch
{
    public static bool IsMatch(object? value, object? parameter) =>
        value is int number &&
        int.TryParse(parameter?.ToString(), out int expected) &&
        number == expected;
}

/// <summary>
/// Returns a translucent accent brush when the bound int equals the parameter, else transparent.
/// Used to tint the header of the column matching the selected month.
/// </summary>
public class MonthMatchToBrushConverter : IValueConverter
{
    public static readonly MonthMatchToBrushConverter Instance = new();

    private static readonly IBrush s_highlight = new SolidColorBrush(Color.Parse("#334CAF50"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        MonthColumnMatch.IsMatch(value, parameter) ? s_highlight : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Returns a 2px border when the bound int equals the parameter, else no border. Used to draw an
/// accent ring around the dropdown in the column matching the selected month.
/// </summary>
public class MonthMatchToThicknessConverter : IValueConverter
{
    public static readonly MonthMatchToThicknessConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        MonthColumnMatch.IsMatch(value, parameter) ? new Thickness(2) : new Thickness(0);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
