namespace CLabs.Prompts;

/// <summary>
/// Interactive multi-select prompt with grouped items, smooth scrolling,
/// and reactive dependency highlighting. Uses ANSI escape codes for
/// flicker-free rendering with no external dependencies.
/// </summary>
public static class MultiSelect
{
    internal record Entry(string Name, string? Group, bool IsHeader);

    // ANSI sequences — relative movement only, no absolute positioning
    private const string Reset = "\x1b[0m";
    private const string Green = "\x1b[32m";
    private const string Cyan = "\x1b[36m";
    private const string Yellow = "\x1b[33;1m";
    private const string White = "\x1b[37;1m";
    private const string Dim = "\x1b[2m";
    private const string ClearLine = "\x1b[2K";

    /// <summary>
    /// Display an interactive multi-select prompt with grouped items.
    /// Returns the list of selected item names, or an empty list if cancelled.
    /// </summary>
    /// <param name="groups">Items organised into named groups.</param>
    /// <param name="options">Optional configuration for exclusions, dependency resolver, and display.</param>
    public static List<string> Prompt(
        (string Group, string[] Items)[] groups,
        MultiSelectOptions? options = null)
    {
        options ??= new MultiSelectOptions();
        var exclude = options.Exclude ?? [];

        // Build flat entry list
        var entries = new List<Entry>();
        var selectableIndices = new List<int>();

        foreach (var (group, items) in groups)
        {
            var available = items.Where(i => !exclude.Contains(i)).ToArray();
            if (available.Length == 0) continue;

            entries.Add(new Entry("", group, IsHeader: true));
            foreach (var item in available)
            {
                selectableIndices.Add(entries.Count);
                entries.Add(new Entry(item, group, IsHeader: false));
            }
        }

        if (selectableIndices.Count == 0)
            return [];

        var selected = new HashSet<string>();
        var cursorPos = 0;
        var scrollOffset = 0;

        var viewportHeight = Math.Clamp(
            Console.WindowHeight - options.ChromeLines,
            Math.Min(options.MinViewportHeight, entries.Count),
            entries.Count);

        Console.CursorVisible = false;

        try
        {
            // Initial frame
            scrollOffset = AdjustScroll(entries, selectableIndices, cursorPos, scrollOffset, viewportHeight);
            var renderedLines = RenderFrame(entries, selectableIndices, selected, cursorPos, viewportHeight, scrollOffset, options);
            WriteLines(renderedLines);

            while (true)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow or ConsoleKey.K:
                        cursorPos = (cursorPos - 1 + selectableIndices.Count) % selectableIndices.Count;
                        break;

                    case ConsoleKey.DownArrow or ConsoleKey.J:
                        cursorPos = (cursorPos + 1) % selectableIndices.Count;
                        break;

                    case ConsoleKey.Spacebar:
                        var name = entries[selectableIndices[cursorPos]].Name;
                        if (!selected.Remove(name))
                            selected.Add(name);
                        break;

                    case ConsoleKey.A:
                        if (selected.Count == selectableIndices.Count)
                            selected.Clear();
                        else
                            foreach (var idx in selectableIndices)
                                selected.Add(entries[idx].Name);
                        break;

                    case ConsoleKey.Enter:
                        return selected.ToList();

                    case ConsoleKey.Escape:
                        return [];

                    default:
                        continue;
                }

                scrollOffset = AdjustScroll(entries, selectableIndices, cursorPos, scrollOffset, viewportHeight);
                var newLines = RenderFrame(entries, selectableIndices, selected, cursorPos, viewportHeight, scrollOffset, options);
                MoveUpAndRewrite(renderedLines.Count, newLines);
                renderedLines = newLines;
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private static List<string> RenderFrame(
        List<Entry> entries,
        List<int> selectableIndices,
        HashSet<string> selected,
        int cursorPos,
        int viewportHeight,
        int scrollOffset,
        MultiSelectOptions options)
    {
        var cursorEntryIndex = selectableIndices[cursorPos];

        // Resolve deps via caller-provided resolver
        var deps = new Dictionary<string, string>();
        if (selected.Count > 0 && options.DependencyResolver != null)
            deps = options.DependencyResolver(selected);

        var scrollStart = scrollOffset;
        var scrollEnd = Math.Min(scrollOffset + viewportHeight, entries.Count);

        var lines = new List<string>();

        // Colour key header
        lines.Add($"  {Green}\u25cf{Reset} {Dim}{options.SelectedLabel}{Reset}    {Cyan}\u25cf{Reset} {Dim}{options.DependencyLabel}{Reset}    {Dim}\u2191\u2193 navigate  space toggle  a all  enter confirm{Reset}");
        lines.Add("");

        // Scroll-up indicator (always one line for consistent frame height)
        lines.Add(scrollStart > 0 ? $"    {Dim}\u2191 more{Reset}" : "");

        // Viewport entries
        for (var i = scrollStart; i < scrollEnd; i++)
        {
            var entry = entries[i];
            if (entry.IsHeader)
            {
                lines.Add($"  {Yellow}{entry.Group}{Reset}");
            }
            else
            {
                var isCursor = i == cursorEntryIndex;
                var isSelected = selected.Contains(entry.Name);
                var isDep = deps.ContainsKey(entry.Name);

                var pointer = isCursor ? $"  {White}>{Reset} " : "    ";
                string checkbox, label;
                var suffix = "";

                if (isSelected)
                {
                    checkbox = $"{Green}\u2713{Reset}  ";
                    label = $"{Green}{entry.Name}{Reset}";
                }
                else if (isDep)
                {
                    checkbox = $"{Cyan}+{Reset}  ";
                    label = $"{Cyan}{entry.Name}{Reset}";
                    var reason = deps[entry.Name];
                    if (!string.IsNullOrEmpty(reason))
                        suffix = $"  {Dim}\u2190 {reason}{Reset}";
                }
                else
                {
                    checkbox = $"{Dim}\u2022{Reset}  ";
                    label = isCursor ? $"{White}{entry.Name}{Reset}" : $"{Dim}{entry.Name}{Reset}";
                }

                lines.Add($"{pointer}{checkbox}{label}{suffix}");
            }
        }

        // Scroll-down indicator (always one line)
        lines.Add(scrollEnd < entries.Count ? $"    {Dim}\u2193 more{Reset}" : "");

        // Summary
        lines.Add("");
        if (selected.Count > 0)
        {
            var s = $"    {Green}{selected.Count} selected{Reset}";
            if (deps.Count > 0)
                s += $" {Dim}+{Reset} {Cyan}{deps.Count} dependency(s){Reset}";
            lines.Add(s);
        }
        else
        {
            lines.Add($"    {Dim}none selected{Reset}");
        }

        return lines;
    }

    internal static int AdjustScroll(
        List<Entry> entries, List<int> selectableIndices,
        int cursorPos, int currentOffset, int viewportHeight)
    {
        if (entries.Count <= viewportHeight)
            return 0;

        var cursorEntryIndex = selectableIndices[cursorPos];
        var offset = currentOffset;

        while (cursorEntryIndex >= offset + viewportHeight)
            offset++;

        while (cursorEntryIndex < offset)
            offset--;

        // Include group header if we'd start mid-group
        if (offset > 0 && !entries[offset].IsHeader)
        {
            for (var i = offset - 1; i >= 0; i--)
            {
                if (entries[i].IsHeader)
                {
                    if (cursorEntryIndex < i + viewportHeight)
                        offset = i;
                    break;
                }
            }
        }

        offset = Math.Clamp(offset, 0, entries.Count - viewportHeight);
        return offset;
    }

    private static void WriteLines(List<string> lines)
    {
        foreach (var line in lines)
            Console.Write($"{ClearLine}{line}\n");
    }

    private static void MoveUpAndRewrite(int previousLineCount, List<string> newLines)
    {
        if (previousLineCount > 0)
            Console.Write($"\x1b[{previousLineCount}A");

        foreach (var line in newLines)
            Console.Write($"\r{ClearLine}{line}\n");

        var extraLines = previousLineCount - newLines.Count;
        for (var i = 0; i < extraLines; i++)
            Console.Write($"{ClearLine}\n");

        if (extraLines > 0)
            Console.Write($"\x1b[{extraLines}A");
    }
}
