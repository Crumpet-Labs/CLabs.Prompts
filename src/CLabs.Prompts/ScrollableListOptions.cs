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
    /// Lines of non-list content to subtract from terminal height. Default: 10.
    /// </summary>
    public int ChromeLines { get; init; } = 10;

    /// <summary>
    /// Minimum viewport height in rows. Default: 6.
    /// </summary>
    public int MinViewportHeight { get; init; } = 6;
}
