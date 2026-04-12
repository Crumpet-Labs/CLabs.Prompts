namespace CLabs.Prompts;

/// <summary>
/// Configuration options for <see cref="ScrollableList.Show"/>.
/// </summary>
public class ScrollableListOptions
{
    /// <summary>
    /// Title shown above the list. Optional.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Minimum viewport height in rows. Default: 6.
    /// </summary>
    public int MinViewportHeight { get; init; } = 6;
}
