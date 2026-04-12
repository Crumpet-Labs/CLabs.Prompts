# Usage Guide

## Basic Multi-Select

The simplest usage — grouped items with no dependency tracking:

```csharp
using CLabs.Prompts;

var selected = MultiSelect.Prompt(
    [
        ("Frontend", new[] { "react", "vue", "svelte" }),
        ("Backend", new[] { "express", "fastapi", "hono" }),
        ("Database", new[] { "postgres", "redis", "mongo" }),
    ]);
```

This displays a full interactive picker:

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
  Database
    •  postgres
    •  redis
    •  mongo

    none selected
```

The user navigates with arrow keys (or j/k), toggles with space, selects all with `a`, confirms with enter, or cancels with escape.

Returns a `List<string>` of selected item names, or an empty list if cancelled.

## Excluding Items

Pass items that should not appear in the picker (e.g. already installed):

```csharp
var selected = MultiSelect.Prompt(
    [
        ("Packages", new[] { "auth", "database", "cache", "queue" }),
    ],
    new MultiSelectOptions
    {
        Exclude = new HashSet<string> { "auth", "database" } // already installed
    });
```

Only `cache` and `queue` will appear. Empty groups are hidden automatically.

## Reactive Dependency Highlighting

The key feature. Provide a `DependencyResolver` function that gets called on every selection change. It receives the currently selected items and returns a dictionary of `{dependency → reason}` for items that should be highlighted as auto-included.

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

            // auth requires framework and logging
            if (selectedItems.Contains("auth"))
            {
                deps.TryAdd("framework", "auth");
                deps.TryAdd("logging", "auth");
            }

            // payments requires auth and config
            if (selectedItems.Contains("payments"))
            {
                deps.TryAdd("auth", "payments");
                deps.TryAdd("config", "payments");
            }

            // Remove items the user explicitly selected (they're not deps)
            foreach (var s in selectedItems)
                deps.Remove(s);

            return deps;
        }
    });
```

When the user toggles `payments`:

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

Items in cyan with `+` are dependencies. The `← reason` shows which selected item requires them. Toggle `payments` off and they all disappear. This updates instantly on every space press.

## Customising Labels

```csharp
var selected = MultiSelect.Prompt(groups, new MultiSelectOptions
{
    SelectedLabel = "chosen",        // default: "selected"
    DependencyLabel = "required",    // default: "dependency"
});
```

Changes the colour key at the top of the picker.

## Adjusting Viewport Size

The picker computes viewport height from the terminal size. If your CLI has extra headers or footers, adjust the chrome calculation:

```csharp
var selected = MultiSelect.Prompt(groups, new MultiSelectOptions
{
    ChromeLines = 20,       // lines of non-picker content (default: 14)
    MinViewportHeight = 8,  // minimum rows to show (default: 6)
});
```

## Scrolling

When the list is taller than the viewport, the picker scrolls automatically as the cursor moves. Scroll indicators (`↑ more` / `↓ more`) appear at the edges. Scrolling is one item at a time — no jarring jumps.

Group headers stay attached to their first item when scrolling.

## Keyboard Controls

| Key | Action |
|---|---|
| `↑` / `k` | Move cursor up |
| `↓` / `j` | Move cursor down |
| `Space` | Toggle selection |
| `a` | Select all / deselect all |
| `Enter` | Confirm selection |
| `Escape` | Cancel (returns empty list) |

## Non-Interactive Environments

If `Console.IsInputRedirected` is true (piped input, CI), you should skip the picker and fall back to a different input method. The library does not handle this automatically — check before calling `Prompt`.

```csharp
if (Console.IsInputRedirected)
{
    Console.WriteLine("Non-interactive mode. Use --packages flag instead.");
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

**Parameters:**
- `groups` — Items organised into named groups. Each group has a name and an array of item strings.
- `options` — Optional configuration (see `MultiSelectOptions`).

**Returns:** List of selected item names. Empty if cancelled or nothing selected.

### `MultiSelectOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Exclude` | `HashSet<string>?` | `null` | Items to hide from the picker |
| `DependencyResolver` | `Func<IReadOnlySet<string>, Dictionary<string, string>>?` | `null` | Reactive dependency resolver |
| `SelectedLabel` | `string` | `"selected"` | Colour key label for selected items |
| `DependencyLabel` | `string` | `"dependency"` | Colour key label for dependency items |
| `ChromeLines` | `int` | `14` | Lines of non-picker content to subtract from terminal height |
| `MinViewportHeight` | `int` | `6` | Minimum viewport rows |

## Technical Details

- **No external dependencies** — pure .NET, no NuGet packages required
- **ANSI escape codes** — uses relative cursor movement (`\x1b[{n}A`) for flicker-free rendering. No `Console.SetCursorPosition` (avoids buffer overflow on Windows)
- **Constant frame height** — scroll indicators always occupy one line (empty when not needed) so the total output line count never changes between frames
- **Stateful scrolling** — viewport offset persists between frames and adjusts by exactly 1 entry when the cursor goes out of bounds
