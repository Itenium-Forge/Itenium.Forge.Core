Itenium.Forge.Core
==================

```sh
dotnet add package Itenium.Forge.Core
```

Provides core types shared across all Forge packages (`ForgeSettings`, `IForgeSettings`).

## Code quality analyzers

Installing any Forge package activates three analyzers automatically — no explicit reference needed.
Rules are `suggestion` by default: visible as IDE hints, never break the build.

| Analyzer | Status | Decision |
|---|---|---|
| **Microsoft.CodeAnalysis.NetAnalyzers** | Active | Modern replacement for FxCop. Bundled in the .NET SDK; added explicitly to pin the latest version and flow transitively via Core |
| **Meziantou.Analyzer** | Active | Broad quality rules; flows transitively via Core |
| **SonarAnalyzer.CSharp** | Active | Security and code smell rules; flows transitively via Core |
| Microsoft.VisualStudio.Threading.Analyzers | Evaluate | VSTHRD* async/threading rules — catches `async void`, wrong `Task` usage. Complements NetAnalyzers in areas CA2007 misses |
| NUnit.Analyzers | Evaluate | We don't want to force a testing framework, but the analyzers could be interesting. CA1707 false positives show the friction — would need a test-only config layer |
| Roslynator.Analyzers | Evaluate | Broad overlap with the three active analyzers. Would require a full triage pass to suppress duplicates before enabling |
| ErrorProne.NET | Evaluate | Could catch subtle correctness bugs. Less mature ecosystem, worth monitoring |
| DevSkim | Evaluate | Security-focused, up and coming. More relevant once the codebase has more surface area to scan |
| Microsoft.CodeAnalysis.BannedApiAnalyzers | Evaluate | Only useful if the team wants to enforce a banned API list — no current need |
| FxCop / StyleCop | Obsolete | Superseded by Microsoft.CodeAnalysis.NetAnalyzers |
| SecurityCodeScan | Obsolete | No longer actively maintained |

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
