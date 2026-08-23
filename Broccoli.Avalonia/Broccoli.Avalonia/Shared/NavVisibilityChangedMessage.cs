namespace Broccoli.Avalonia.Shared;

/// <summary>
/// Raised after the visibility of the Seasonality navigation tab changes, so the shell can
/// update its visible menu items.
/// </summary>
public sealed record NavVisibilityChangedMessage(bool ShowSeasonalityTab);
