namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Retries file-system operations that can transiently fail because a file is locked or in use
/// by another process (e.g. a recipe or snapshot being viewed/edited externally). The original
/// exception is rethrown if every attempt fails.
/// </summary>
public static class FileSystemRetry
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromMilliseconds(250);

    /// <summary>Runs <paramref name="operation"/>, retrying transient lock errors a few times.</summary>
    public static void Execute(Action operation)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                operation();
                return;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                if (attempt >= MaxAttempts)
                {
                    throw;
                }

                Thread.Sleep(DelayBetweenAttempts);
            }
        }
    }

    private static bool IsTransient(Exception ex) => ex is IOException or UnauthorizedAccessException;
}
