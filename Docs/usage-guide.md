# Usage Guide

## MultiSelect

### Basic usage

Grouped items with no dependency tracking:

```csharp
using CLabs.Prompts;

var selected = MultiSelect.Prompt(
    [
        ("Frontend", new[] { "react", "vue", "svelte" }),
        ("Backend", new[] { "express", "fastapi", "hono" }),
        ("Database", new[] { "postgres", "redis", "mongo" }),
    ]);
```

Displays an interactive picker:

```
  ● selected    ● dependency    ↑↓ navigate  space toggle  a all  enter confirm

  Frontend
  > •  react
    •  vue
    •  svelte
  Backend
    •  express
    •  fastapi
    •  hono

    none selected
```

Navigate with arrow keys (or j/k), toggle with space, select all with `a`, confirm with enter, cancel with escape.

Returns a `List<string>` of selected item names, or an empty list if cancelled.

### Excluding items

Hide items from the picker (e.g. already installed):

```csharp
var selected = MultiSelect.Prompt(
    [
        ("Packages", new[] { "auth", "database", "cache", "queue" }),
    ],
    new MultiSelectOptions
    {
        Exclude = new HashSet<string> { "auth", "database" }
    });
```

Only `cache` and `queue` will appear. Empty groups are hidden automatically.

### Reactive dependency highlighting

Provide a `DependencyResolver` function that gets called on every selection change. It receives the currently selected items and returns `{dependency name -> reason}` for items that should be highlighted as auto-included:

```csharp
var selected = MultiSelect.Prompt(
    [
        ("Core", new[] { "framework", "logging", "config" }),
        ("Features", new[] { "auth", "payments", "notifications" }),
    ],
    new MultiSelectOptions
    {
        DependencyResolver = selectedItems =>
        {
            var deps = new Dictionary<string, string>();

            if (selectedItems.Contains("auth"))
            {
                deps.TryAdd("framework", "auth");
                deps.TryAdd("logging", "auth");
            }

            if (selectedItems.Contains("payments"))
            {
                deps.TryAdd("auth", "payments");
                deps.TryAdd("config", "payments");
            }

            foreach (var s in selectedItems)
                deps.Remove(s);

            return deps;
        }
    });
```

When you toggle `payments`:

```
  Core
    +  framework  ← auth
    +  logging  ← auth
    +  config  ← payments
  Features
    +  auth  ← payments
    ✓  payments
    •  notifications

    1 selected + 4 dependency(s)
```

Items in cyan with `+` are dependencies. The `← reason` shows which selected item requires them. Toggle `payments` off and they all disappear instantly.

### Customising labels

```csharp
var selected = MultiSelect.Prompt(groups, new MultiSelectOptions
{
    SelectedLabel = "chosen",
    DependencyLabel = "required",
});
```

### Scrolling

The picker shows up to 20 entries at a time. When the list is longer, it scrolls one item at a time as the cursor moves. Scroll indicators (`↑ more` / `↓ more`) appear at the edges. Group headers stay attached to their first item.

## ScrollableList

Read-only scrollable list with a detail pane for the highlighted item:

```csharp
using CLabs.Prompts;

ScrollableList.Show(
    [
        ("inventory — Generic container system", "com.clabs.inventory  |  Dependencies: utility"),
        ("knife — Key-value state store", "com.clabs.knife  |  Dependencies: eda"),
    ]);
```

```
  ↑↓ navigate  enter/esc exit

  > inventory — Generic container system
    knife — Key-value state store

    com.clabs.inventory  |  Dependencies: utility
```

The first string in each tuple is the list label, the second is the detail shown below when highlighted. Navigate with arrow keys, exit with enter or escape.

Shows up to 15 items at a time with the same smooth scrolling as MultiSelect.

## Keyboard Controls

| Key | Action |
|---|---|
| `↑` / `k` | Move cursor up |
| `↓` / `j` | Move cursor down |
| `Space` | Toggle selection (MultiSelect only) |
| `a` | Select all / deselect all (MultiSelect only) |
| `Enter` | Confirm (MultiSelect) / Exit (ScrollableList) |
| `Escape` | Cancel / Exit |
| `q` | Exit (ScrollableList only) |

## Non-Interactive Environments

Check `Console.IsInputRedirected` before calling prompts — they require a real terminal:

```csharp
if (Console.IsInputRedirected)
{
    Console.WriteLine("Non-interactive mode.");
    return;
}

var selected = MultiSelect.Prompt(groups);
```

## API Reference

### `MultiSelect.Prompt`

```csharp
public static List<string> Prompt(
    (string Group, string[] Items)[] groups,
    MultiSelectOptions? options = null)
```

Returns selected item names, or empty list if cancelled.

### `MultiSelectOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Exclude` | `HashSet<string>?` | `null` | Items to hide from the picker |
| `DependencyResolver` | `Func<IReadOnlySet<string>, Dictionary<string, string>>?` | `null` | Reactive dependency resolver |
| `SelectedLabel` | `string` | `"selected"` | Colour key label for selected items |
| `DependencyLabel` | `string` | `"dependency"` | Colour key label for dependency items |
| `MinViewportHeight` | `int` | `6` | Minimum viewport rows |

### `ScrollableList.Show`

```csharp
public static void Show(
    (string Label, string Detail)[] items,
    ScrollableListOptions? options = null)
```

### `ScrollableListOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string?` | `null` | Optional title above the list |
| `MinViewportHeight` | `int` | `6` | Minimum viewport rows |

## Technical Details

- **Zero dependencies** -- pure .NET, no NuGet packages
- **UTF-8 output** -- sets `Console.OutputEncoding = UTF8` for Unicode symbols (checkmarks, bullets, arrows)
- **ANSI rendering** -- uses relative cursor movement (`\x1b[{n}A`) for flicker-free updates. No `Console.SetCursorPosition` (avoids Windows buffer overflow)
- **VT processing** -- enables `ENABLE_VIRTUAL_TERMINAL_PROCESSING` on Windows via P/Invoke for reliable ANSI support
- **Fixed viewport** -- caps at 20 entries (MultiSelect) or 15 (ScrollableList). Terminal scrolls naturally for longer content
- **Stateful scrolling** -- viewport offset adjusts by exactly 1 entry when the cursor goes out of bounds
- **Constant frame height** -- scroll indicators always occupy one line so total output never changes between frames
