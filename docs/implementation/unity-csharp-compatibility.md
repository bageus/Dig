# Unity C# compatibility

## Compiler baseline

The Unity presentation host compiles the shared local UPM package with the C# 9 language surface available in the selected Unity 6 project. The engine-independent solution therefore uses the same effective baseline:

```xml
<LangVersion>9.0</LangVersion>
<ImplicitUsings>disable</ImplicitUsings>
```

The .NET build intentionally does not use a newer language version for Domain, Application, Infrastructure, Presentation or Headless. A construct that cannot compile in the Unity host must also fail in normal CI.

## Source rules

C# files shared with Unity use block-scoped namespaces:

```csharp
namespace Dig.Domain.World
{
    public sealed class Example
    {
    }
}
```

Unity-visible source under `src`, `tests` and `unity` must not use:

- file-scoped namespaces;
- global using directives;
- record structs;
- SDK implicit usings.

Every file declares the framework namespaces it consumes. This keeps Unity compilation and the .NET build equivalent instead of relying on generated imports that Unity does not provide.

## Quality enforcement

`tools/quality/check_quality.py` verifies:

- `Directory.Build.props` remains on C# 9;
- implicit usings remain disabled;
- Unity-visible files contain no file-scoped namespaces, global usings or record structs;
- existing architecture, domain-boundary and file-size rules still pass.

GitHub Actions then performs a Release build with warnings as errors, the full test suite, the normal headless scenario and both deterministic soak profiles.

## Migration result

Issue #47 mechanically converted the existing source from file-scoped to block-scoped namespaces and added explicit framework imports. Runtime state ownership, command/event/query contracts and simulation behavior were not changed.

The first complete C# 9 CI run retained the established deterministic hashes:

```text
standard: B315282B332B67B4EEE68D3B3C59D997013C014A947B534106DA7FD75EC04480
large:    42B798277A05A1099E5C8DF3EC59F0B8931AE8C49BB6C10AD11238F2B8D0CC99
```

Both profiles completed with deterministic replay matched, no invariant violations and no performance-budget violations.

## Refreshing an existing Unity checkout

After pulling the compatibility change:

1. close Unity or allow it to finish its current compile;
2. update the repository to the latest `main`;
3. reopen `unity/Dig.Unity`;
4. allow the local package and scripts to reimport;
5. clear the Console and confirm that the previous CS8773 errors do not return.

If Unity retains stale compiler output, use **Assets > Reimport All** or close the editor and delete the project's `Library` directory before reopening. `Library` is generated Unity state and is not committed.
