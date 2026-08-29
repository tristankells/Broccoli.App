using System.Globalization;
using Avalonia.Data.Converters;

namespace Broccoli.Avalonia.Shared.Converters;

/// <summary>
/// Bridges Avalonia's <c>DateTimeOffset?</c> controls (DatePicker/CalendarDatePicker) and the
/// view models' <c>DateTime?</c> properties.
/// </summary>
public class DateTimeOffsetConverter : IValueConverter
{
    public static readonly DateTimeOffsetConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dateTime ? new DateTimeOffset(dateTime) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTimeOffset offset ? offset.Date : value;
}
