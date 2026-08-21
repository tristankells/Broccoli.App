using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Tests.Storage;

[TestClass]
public class FileSystemRetryTests
{
    [TestMethod]
    public void Execute_SucceedsOnFirstAttempt()
    {
        int calls = 0;

        FileSystemRetry.Execute(() => calls++);

        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void Execute_RetriesTransientFailure()
    {
        int calls = 0;

        FileSystemRetry.Execute(() =>
        {
            calls++;
            if (calls < 2)
            {
                throw new IOException("file locked");
            }
        });

        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void Execute_RethrowsAfterMaxAttempts()
    {
        int calls = 0;

        Assert.ThrowsExactly<IOException>(() => FileSystemRetry.Execute(() =>
        {
            calls++;
            throw new IOException("file locked");
        }));

        Assert.AreEqual(3, calls);
    }

    [TestMethod]
    public void Execute_DoesNotRetryNonTransientExceptions()
    {
        int calls = 0;

        Assert.ThrowsExactly<InvalidOperationException>(() => FileSystemRetry.Execute(() =>
        {
            calls++;
            throw new InvalidOperationException("boom");
        }));

        Assert.AreEqual(1, calls);
    }
}
