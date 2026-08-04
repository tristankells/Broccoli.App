using Avalonia.Data.Converters;
using Markdig;
using System.Globalization;

namespace Broccoli.Avalonia.Shared.Converters;

public class MarkdownToPlainTextConverter : IValueConverter
{
    public static readonly MarkdownToPlainTextConverter Instance = new();

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return Markdown.ToPlainText(text, Pipeline);
        }
        catch
        {
            return text;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
