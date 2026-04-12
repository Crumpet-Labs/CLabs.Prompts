namespace CLabs.Prompts;

/// <summary>
/// Read-only scrollable list with a detail pane for the highlighted item.
/// Arrow keys to scroll, Enter/Escape to exit.
/// Uses the same ANSI rendering approach as <see cref="MultiSelect"/>.
/// </summary>
public static class ScrollableList
{
    private const string Reset = "\x1b[0m";
    private const string Green = "\x1b[32m";
    private const string Cyan = "\x1b[36m";
    private const string Yellow = "\x1b[33;1m";
    private const string White = "\x1b[37;1m";
    private const string Dim = "\x1b[2m";
    private const string ClearLine = "\x1b[2K";

    /// <summary>
    /// Display a scrollable list. Each item has a label (shown in the list)
    /// and a detail string (shown below when highlighted).
    /// </summary>
    /// <param name="items">Array of (Label, Detail) tuples.</param>
    /// <param name="options">Optional display configuration.</param>
    public static void Show(
        (string Label, string Detail)[] items,
        ScrollableListOptions? options = null)
    {
        if (items.Length == 0)
        {
            Console.WriteLine("  (empty)");
            return;
        }

        options ??= new ScrollableListOptions();

        // Detail pane takes 3 lines (blank + detail + blank), plus instructions = 5 extra
        var viewportHeight = Math.Clamp(
            Console.WindowHeight - options.ChromeLines - 5,
            Math.Min(options.MinViewportHeight, items.Length),
            items.Length);

        var cursor = 0;
        var scrollOffset = 0;

        Console.CursorVisible = false;

        try
        {
            var renderedLines = RenderFrame(items, cursor, viewportHeight, scrollOffset, options);
            WriteLines(renderedLines);

            while (true)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow or ConsoleKey.K:
                        cursor = (cursor - 1 + items.Length) % items.Length;
                        break;

                    case ConsoleKey.DownArrow or ConsoleKey.J:
                        cursor = (cursor + 1) % items.Length;
                        break;

                    case ConsoleKey.Enter or ConsoleKey.Escape or ConsoleKey.Q:
                        return;

                    default:
                        continue;
                }

                scrollOffset = AdjustScroll(items.Length, cursor, scrollOffset, viewportHeight);
                var newLines = RenderFrame(items, cursor, viewportHeight, scrollOffset, options);
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
        (string Label, string Detail)[] items,
        int cursor,
        int viewportHeight,
        int scrollOffset,
        ScrollableListOptions options)
    {
        var lines = new List<string>();

        // Instructions
        lines.Add($"  {Dim}\u2191\u2193 navigate  enter/esc exit{Reset}");
        lines.Add("");

        // Scroll-up indicator
        lines.Add(scrollOffset > 0 ? $"    {Dim}\u2191 more{Reset}" : "");

        // Items
        var scrollEnd = Math.Min(scrollOffset + viewportHeight, items.Length);
        for (var i = scrollOffset; i < scrollEnd; i++)
        {
            var isCursor = i == cursor;
            var (label, _) = items[i];

            var pointer = isCursor ? $"  {White}>{Reset} " : "    ";
            var text = isCursor ? $"{White}{label}{Reset}" : $"{Dim}{label}{Reset}";
            lines.Add($"{pointer}{text}");
        }

        // Scroll-down indicator
        lines.Add(scrollEnd < items.Length ? $"    {Dim}\u2193 more{Reset}" : "");

        // Detail pane
        lines.Add("");
        var detail = items[cursor].Detail;
        if (!string.IsNullOrEmpty(detail))
            lines.Add($"    {Cyan}{detail}{Reset}");
        else
            lines.Add("");

        return lines;
    }

    private static int AdjustScroll(int itemCount, int cursor, int currentOffset, int viewportHeight)
    {
        if (itemCount <= viewportHeight)
            return 0;

        var offset = currentOffset;

        while (cursor >= offset + viewportHeight)
            offset++;
        while (cursor < offset)
            offset--;

        offset = Math.Clamp(offset, 0, itemCount - viewportHeight);
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
