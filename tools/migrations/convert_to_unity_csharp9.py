#!/usr/bin/env python3
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOTS = ("src", "tests", "unity")
FILE_SCOPED_NAMESPACE = re.compile(
    r"^(?P<indent>[ \t]*)namespace\s+(?P<name>[A-Za-z_][A-Za-z0-9_.]*)\s*;[ \t]*$",
    re.MULTILINE,
)

IMPLICIT_USINGS: tuple[tuple[str, re.Pattern[str]], ...] = (
    (
        "System",
        re.compile(
            r"\b(?:ArgumentException|ArgumentNullException|ArgumentOutOfRangeException|"
            r"InvalidOperationException|NotSupportedException|Exception|StringComparison|"
            r"StringComparer|Array|Math|Enum|HashCode|Guid|DateTime|TimeSpan|Console|"
            r"Environment|Span|ReadOnlySpan|Memory|ReadOnlyMemory|IEquatable|IComparable|"
            r"IDisposable|Random|BitConverter|Convert|GC|Type|Attribute|Func|Action)\b"
        ),
    ),
    (
        "System.Collections.Generic",
        re.compile(
            r"\b(?:Dictionary|List|HashSet|IEnumerable|IReadOnlyList|"
            r"IReadOnlyCollection|IReadOnlyDictionary|ICollection|IDictionary|Queue|"
            r"Stack|SortedDictionary|SortedSet|KeyValuePair|Comparer|EqualityComparer|"
            r"IComparer)\s*<"
        ),
    ),
    (
        "System.IO",
        re.compile(
            r"\b(?:File|Directory|Path)\s*\.|\b(?:Stream|FileInfo|DirectoryInfo|"
            r"TextReader|TextWriter|MemoryStream|BinaryReader|BinaryWriter)\b"
        ),
    ),
    (
        "System.Linq",
        re.compile(
            r"\bEnumerable\.|\.(?:OrderBy|OrderByDescending|ThenBy|ThenByDescending|"
            r"Select|Where|ToArray|ToList|ToDictionary|First|FirstOrDefault|Single|"
            r"SingleOrDefault|Any|All|Sum|Max|Min|GroupBy|Distinct|Cast|OfType|"
            r"SequenceEqual|Skip|Take)\s*\("
        ),
    ),
    (
        "System.Net.Http",
        re.compile(r"\b(?:HttpClient|HttpRequestMessage|HttpResponseMessage)\b"),
    ),
    (
        "System.Threading",
        re.compile(r"\b(?:CancellationToken|Interlocked|Monitor|Volatile|Thread)\b"),
    ),
    (
        "System.Threading.Tasks",
        re.compile(r"\b(?:Task|ValueTask)\s*(?:<|\b)"),
    ),
)


def iter_csharp_files() -> list[Path]:
    files: list[Path] = []
    for relative_root in SOURCE_ROOTS:
        source_root = ROOT / relative_root
        if source_root.exists():
            files.extend(source_root.rglob("*.cs"))
    return sorted(
        path
        for path in files
        if "obj" not in path.parts and "bin" not in path.parts and "Library" not in path.parts
    )


def add_required_usings(text: str) -> str:
    required: list[str] = []
    for namespace, pattern in IMPLICIT_USINGS:
        directive = f"using {namespace};"
        if directive not in text and pattern.search(text):
            required.append(directive)

    if not required:
        return text

    lines = text.splitlines()
    insertion = 0
    while insertion < len(lines) and (
        lines[insertion].startswith("//")
        or lines[insertion].startswith("#nullable")
        or not lines[insertion].strip()
    ):
        insertion += 1

    lines[insertion:insertion] = required
    return "\n".join(lines) + ("\n" if text.endswith("\n") else "")


def convert_namespace(text: str, path: Path) -> str:
    matches = list(FILE_SCOPED_NAMESPACE.finditer(text))
    if not matches:
        return text
    if len(matches) != 1:
        raise RuntimeError(f"{path}: expected one file-scoped namespace, found {len(matches)}")

    match = matches[0]
    if match.group("indent"):
        raise RuntimeError(f"{path}: file-scoped namespace is unexpectedly indented")

    converted = text[: match.start()] + f"namespace {match.group('name')}\n{{" + text[match.end() :]
    return converted.rstrip() + "\n}\n"


def update_build_properties() -> bool:
    path = ROOT / "Directory.Build.props"
    text = path.read_text(encoding="utf-8")
    updated = text.replace("<LangVersion>10.0</LangVersion>", "<LangVersion>9.0</LangVersion>")
    updated = updated.replace(
        "<ImplicitUsings>enable</ImplicitUsings>",
        "<ImplicitUsings>disable</ImplicitUsings>",
    )
    if updated == text:
        return False
    path.write_text(updated, encoding="utf-8")
    return True


def main() -> int:
    changed = 0
    converted_namespaces = 0
    for path in iter_csharp_files():
        original = path.read_text(encoding="utf-8-sig")
        updated = convert_namespace(original, path)
        if updated != original:
            converted_namespaces += 1
        updated = add_required_usings(updated)
        if updated != original:
            path.write_text(updated, encoding="utf-8", newline="\n")
            changed += 1

    props_changed = update_build_properties()
    print(
        f"Updated {changed} C# files, converted {converted_namespaces} namespaces, "
        f"Directory.Build.props changed={props_changed}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
