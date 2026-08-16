using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Broccoli.Avalonia.Shared.Converters;

/// <summary>Loads a <see cref="Bitmap"/> from a full file path string, for binding to <c>Image.Source</c>.</summary>
public class FilePathToBitmapConverter : IValueConverter
{
    public static readonly FilePathToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            try
            {
                return new Bitmap(path);
            }
            catch (Exception)
            {
                // Corrupt/unsupported image file — show nothing rather than crashing the page.
                return null;
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
