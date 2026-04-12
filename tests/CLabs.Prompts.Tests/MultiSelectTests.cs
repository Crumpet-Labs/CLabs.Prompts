using Xunit;
using static CLabs.Prompts.MultiSelect;

namespace CLabs.Prompts.Tests;

public class MultiSelectTests
{
    private static List<Entry> Flat(params string[] items) =>
        items.Select(i => new Entry(i, null, false)).ToList();

    private static List<Entry> Grouped(params (string group, string[] items)[] groups)
    {
        var list = new List<Entry>();
        foreach (var (g, items) in groups)
        {
            list.Add(new Entry("", g, true));
            foreach (var item in items)
                list.Add(new Entry(item, g, false));
        }
        return list;
    }

    private static List<int> SelectableIndices(List<Entry> entries) =>
        entries.Select((e, i) => (e, i)).Where(x => !x.e.IsHeader).Select(x => x.i).ToList();

    [Fact]
    public void NoScrollWhenFits()
    {
        var entries = Flat("a", "b", "c");
        var sel = SelectableIndices(entries);
        Assert.Equal(0, AdjustScroll(entries, sel, 0, 0, 10));
        Assert.Equal(0, AdjustScroll(entries, sel, 2, 0, 10));
    }

    [Fact]
    public void ScrollsDownByOne()
    {
        var entries = Flat("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
        var sel = SelectableIndices(entries);
        // Viewport 5, cursor at index 5 (one past viewport)
        var offset = AdjustScroll(entries, sel, 5, 0, 5);
        Assert.Equal(1, offset);
    }

    [Fact]
    public void ScrollsUpByOne()
    {
        var entries = Flat("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
        var sel = SelectableIndices(entries);
        // At offset 5, cursor at index 4
        var offset = AdjustScroll(entries, sel, 4, 5, 5);
        Assert.Equal(4, offset);
    }

    [Fact]
    public void ClampsToEnd()
    {
        var entries = Flat("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
        var sel = SelectableIndices(entries);
        var offset = AdjustScroll(entries, sel, 9, 0, 5);
        Assert.Equal(5, offset); // entries.Count - viewportHeight
    }

    [Fact]
    public void CursorAlwaysWithinViewport()
    {
        var entries = Flat("a", "b", "c", "d", "e", "f", "g", "h");
        var sel = SelectableIndices(entries);

        for (var cursor = 0; cursor < sel.Count; cursor++)
        {
            var offset = AdjustScroll(entries, sel, cursor, 0, 4);
            var entryIdx = sel[cursor];
            Assert.True(entryIdx >= offset, $"Cursor {cursor} (entry {entryIdx}) below offset {offset}");
            Assert.True(entryIdx < offset + 4, $"Cursor {cursor} (entry {entryIdx}) above viewport end {offset + 4}");
        }
    }

    [Fact]
    public void GroupHeaderIncludedWhenScrolling()
    {
        var entries = Grouped(
            ("Group A", ["a1", "a2", "a3"]),
            ("Group B", ["b1", "b2", "b3"]));
        var sel = SelectableIndices(entries);

        // Cursor on b1 (index 5 in entries: header-a, a1, a2, a3, header-b, b1)
        // Viewport 4 — should include header-b
        var offset = AdjustScroll(entries, sel, 3, 0, 4); // sel[3] = b1
        Assert.True(entries[offset].IsHeader || offset <= sel[3]);
    }
}
