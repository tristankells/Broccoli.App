using Avalonia.Data.Converters;
using System.Collections;
using System.Globalization;

namespace Broccoli.Avalonia.Shared.Converters;

/// <summary>Joins an <see cref="IEnumerable"/> of strings (e.g. <c>Recipe.Tags</c>) with commas for display.</summary>
public class StringJoinConverter : IValueConverter
{
    public static readonly StringJoinConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable items and not string)
        {
            return string.Join(", ", items.Cast<object>());
        }

        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
