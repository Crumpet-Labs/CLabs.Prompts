# CLabs.Prompts

Interactive terminal prompts for .NET CLI tools. Features a multi-select picker with grouped items, smooth scrolling, and reactive dependency highlighting.

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

### Project reference (monorepo)

```xml
<ProjectReference Include="..\CLabs.Prompts\src\CLabs.Prompts\CLabs.Prompts.csproj" />
```

## Quick Start

```csharp
using CLabs.Prompts;

var selected = MultiSelect.Prompt(
    [
        ("Fruits", new[] { "apple", "banana", "cherry" }),
        ("Vegetables", new[] { "carrot", "broccoli", "spinach" }),
    ]);

Console.WriteLine($"You selected: {string.Join(", ", selected)}");
```

## Documentation

- [Usage Guide](Docs/usage-guide.md) -- Full API reference and examples

## Requirements

- .NET 9.0+
- A terminal that supports ANSI escape codes (all modern terminals)

## License

MIT
