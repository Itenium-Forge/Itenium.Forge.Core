Itenium.Forge.Core
==================

```sh
dotnet add package Itenium.Forge.Core
```

Provides core types shared across all Forge packages (`ForgeSettings`, `IForgeSettings`).

## Code quality analyzers

Installing any Forge package activates **Meziantou.Analyzer** and **SonarAnalyzer.CSharp**
automatically — no explicit reference needed. Rules are `suggestion` by default: visible as
IDE hints, never break the build.

To opt into strict enforcement (critical rules become warnings, treated as errors):

```xml
<!-- Directory.Build.props or .csproj -->
<PropertyGroup>
  <EnforceForgeRules>true</EnforceForgeRules>
</PropertyGroup>
```

Or per build: `dotnet build -p:EnforceForgeRules=true`

## Overriding analyzer rules

To promote or silence individual rules in your project, create a `.globalconfig` file with
a higher `global_level` (higher level wins):

```ini
is_global = true
global_level = 1

dotnet_diagnostic.MA0006.severity = warning   # promote to warning
dotnet_diagnostic.CA1707.severity = none      # silence entirely
```
