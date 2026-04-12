namespace CLabs.Prompts;

/// <summary>
/// Configuration options for <see cref="MultiSelect.Prompt"/>.
/// </summary>
public class MultiSelectOptions
{
    /// <summary>
    /// Items to exclude from the list (e.g. already installed packages).
    /// These won't appear in the picker at all.
    /// </summary>
    public HashSet<string>? Exclude { get; init; }

    /// <summary>
    /// Reactive dependency resolver. Called on every selection change.
    /// Given the set of currently selected items, return a dictionary of
    /// {dependency name → reason string} for items that should be highlighted
    /// as auto-included dependencies.
    /// </summary>
    public Func<IReadOnlySet<string>, Dictionary<string, string>>? DependencyResolver { get; init; }

    /// <summary>
    /// Label shown in the colour key for selected items. Default: "selected".
    /// </summary>
    public string SelectedLabel { get; init; } = "selected";

    /// <summary>
    /// Label shown in the colour key for dependency items. Default: "dependency".
    /// </summary>
    public string DependencyLabel { get; init; } = "dependency";

    /// <summary>
    /// Number of fixed lines above and below the picker (headers, prompts, shell chrome)
    /// subtracted from terminal height to compute viewport size. Default: 14.
    /// </summary>
    public int ChromeLines { get; init; } = 14;

    /// <summary>
    /// Minimum viewport height in entry rows. Default: 6.
    /// </summary>
    public int MinViewportHeight { get; init; } = 6;
}
