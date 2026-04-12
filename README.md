# CLabs.Prompts

Interactive terminal prompts for .NET CLI tools. Zero dependencies.

- **MultiSelect** -- grouped multi-select picker with reactive dependency highlighting and smooth scrolling
- **ScrollableList** -- read-only scrollable list with a detail pane for the highlighted item

## Installation

### From source (local)

```bash
git clone https://github.com/Crumpet-Labs/CLabs.Prompts.git
cd CLabs.Prompts
dotnet pack src/CLabs.Prompts -o nupkg
```

Then reference the `.nupkg` in your project:

```bash
dotnet add package CLabs.Prompts --source ./path/to/nupkg
```

### Project reference

```xml
<ProjectReference Include="..\CLabs.Prompts\src\CLabs.Prompts\CLabs.Prompts.csproj" />
```

## Quick Start

### Multi-Select

```csharp
using CLabs.Prompts;

var selected = MultiSelect.Prompt(
    [
        ("Fruits", new[] { "apple", "banana", "cherry" }),
        ("Vegetables", new[] { "carrot", "broccoli", "spinach" }),
    ]);

Console.WriteLine($"You selected: {string.Join(", ", selected)}");
```

### Scrollable List

```csharp
using CLabs.Prompts;

ScrollableList.Show(
    [
        ("apple — A crisp fruit", "Malus domestica | Available year-round"),
        ("banana — A yellow fruit", "Musa | Rich in potassium"),
        ("cherry — A small stone fruit", "Prunus avium | Seasonal"),
    ]);
```

## Documentation

- [Usage Guide](Docs/usage-guide.md) -- Full API reference and examples

## Requirements

- .NET 9.0+
- A terminal that supports ANSI escape codes (all modern terminals)

## Acknowledgements

The Windows console mode handling was inspired by [Spectre.Console](https://github.com/spectreconsole/spectre.console) (MIT licensed), which uses `SetConsoleMode` with `ENABLE_VIRTUAL_TERMINAL_PROCESSING` for reliable ANSI rendering on Windows.

## License

MIT
