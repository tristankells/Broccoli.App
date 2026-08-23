namespace Broccoli.Avalonia.Shared;

/// <summary>
/// Raised after the produce seasonality dataset changes, so the scoring service and the
/// Seasonality page can refresh their in-memory copies.
/// </summary>
public sealed record SeasonalityDataChangedMessage;
