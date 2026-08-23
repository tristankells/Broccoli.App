using System.Globalization;
using System.Text.RegularExpressions;

namespace Broccoli.Avalonia.Shared;

/// <summary>
/// Conversion and parsing helpers for the energy fields (calories &lt;-&gt; kilojoules).
/// Kilojoules is derived from calories (1 kcal = 4.184 kJ) and is a UI-only value.
/// </summary>
public static class EnergyConversions
{
    /// <summary>Energy in one calorie (kcal), in kilojoules.</summary>
    public const double KilojoulesPerCalorie = 4.184;

    private static readonly Regex s_numberPrefix = new(
        @"^\s*(?<number>[-+]?\d+(?:[.,]\d+)?)",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts the leading number from a value, ignoring trailing unit text such as "kJ", "kcal"
    /// or "cal" (e.g. "739kJ" to 739, "100 kcal" to 100).
    /// </summary>
    public static bool TryParse(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Match match = s_numberPrefix.Match(value);
        if (!match.Success)
        {
            return false;
        }

        string number = match.Groups["number"].Value.Replace(',', '.');
        return double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static double ParseOrDefault(string? value) =>
        TryParse(value, out double d) ? d : 0;

    public static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
