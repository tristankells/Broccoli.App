using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

public interface ISeasonalityService
{
    SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null);
}
