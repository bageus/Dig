from __future__ import annotations

import re
import sys
from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one match, found {count}: {old!r}")
    file.write_text(text.replace(old, new, 1), encoding="utf-8")


def replace_regex_once(path: str, pattern: str, replacement: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    updated, count = re.subn(pattern, replacement, text, count=1, flags=re.DOTALL)
    if count != 1:
        raise SystemExit(f"{path}: expected one regex match, found {count}: {pattern!r}")
    file.write_text(updated, encoding="utf-8")


def update_docs() -> None:
    inventory = "docs/design/resident-inventory-expansion.md"
    replace_once(
        inventory,
        "- группы разделены рамкой и spacing; текстовый заголовок `Cargo 4/6` не отображается;",
        "- группы разделены рамкой и spacing; текстовые заголовки `Weapon` и `Cargo 4/6` не отображаются;",
    )
    replace_once(
        inventory,
        "- каждый compartment строится строго в два горизонтальных ряда: если существует верхняя ячейка колонки, существует и нижняя;",
        "- каждый compartment строится строго в два горизонтальных ряда: если существует верхняя ячейка колонки, существует и нижняя;\n- slot indices проецируются по колонкам: `1` сверху и `2` снизу, затем `3` сверху и `4` снизу следующей колонки, затем `5`/`6`;",
    )
    replace_once(
        inventory,
        "- HUD не показывает `Cargo 4/6`; inventory compartments используют только парные двухрядные сетки `3×2`, `2×2`/`3×2`, `1×2`/`2×2`;",
        "- HUD не показывает текстовые заголовки `Weapon` и `Cargo 4/6`; inventory compartments используют только парные двухрядные сетки `3×2`, `2×2`/`3×2`, `1×2`/`2×2` и заполняются по колонкам `1/2`, `3/4`, `5/6`;",
    )
    replace_once(
        inventory,
        "| 2026-07-30 | Ножны, разгрузка и `weapon.club` появляются отдельными world items; club служит runtime-проверкой Weapon-slot priority и tier switching. | пользователь | #68, #69, #70 |",
        "| 2026-07-30 | Ножны, разгрузка и `weapon.club` появляются отдельными world items; club служит runtime-проверкой Weapon-slot priority и tier switching. | пользователь | #68, #69, #70 |\n| 2026-07-30 | Текстовый заголовок Weapon скрыт; двухрядные inventory grids нумеруются по колонкам: `1/2`, `3/4`, `5/6`. | пользователь | #70 |",
    )

    mushroom = "docs/design/mushroom-growth-and-chopping.md"
    old_workflow = "5. Resident освобождается от несовместимого небоевого direct action, получает route к допустимой work position и идёт к грибу. Work position обязана находиться на той же высоте `Y`, быть соседней по `X` или depth `Z` и иметь полную ровную actor support surface. Вертикальные `Y±1`, shaft-gap и partial-support клетки запрещены. Если боковые `X±1` клетки являются пропастью, resolver обязан рассмотреть поддерживаемые `Z±1` позиции за/перед грибом до blocked result."
    replace_once(
        mushroom,
        old_workflow,
        old_workflow + " Требование полной опоры относится к конечной stationary work position и повторно проверяется перед swing; transit route использует обычную Navigation policy и может включать разрешённые vertical climb, shaft и depth transitions.",
    )

    supported = "docs/implementation/supported-stationary-action-positions-2026-07-30.md"
    replace_once(
        supported,
        "- permits only supported walk/depth transitions for mushroom approach;",
        "- uses ordinary resident Navigation for mushroom travel, including approved climb/shaft/depth transitions, while requiring full support only at the final stationary action cell;",
    )
    replace_once(
        supported,
        "## Evidence\n",
        "## Route regression correction — 2026-07-30\n\nThe initial correction accidentally applied the stationary support invariant to every cell and transition in the travel path. That rejected otherwise valid direct and automatic mushroom jobs whenever the resident had to climb or cross an approved shaft/depth route before reaching a fully supported work cell. The resolver and replanner now validate ordinary Navigation reachability plus full support at the final work position only; support is still revalidated before every swing.\n\n## Evidence\n",
    )

    hud_note = "docs/implementation/hud-basket-placement-and-building-work-position-2026-07-29.md"
    replace_once(
        hud_note,
        "- `DigGameHudCanvas.Inventory` resolves compartment columns from capacity while enforcing exactly two rows and hides the Cargo capacity heading.",
        "- `DigGameHudCanvas.Inventory` resolves compartment columns from capacity, hides Weapon/Cargo text headings and fills exactly two rows column-first (`1/2`, `3/4`, `5/6`).",
    )
    replace_once(
        hud_note,
        "- `2×2` basket and `3×2` large-basket Cargo layout without a Cargo title;",
        "- Main/Cargo/Weapon two-row layouts without Weapon/Cargo text titles and with column-major slot order `1/2`, `3/4`, `5/6`;",
    )


def update_code() -> None:
    inventory_layout = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigGameHudCanvas.InventoryLayout.cs"
    replace_once(inventory_layout, "using UnityEngine;\n", "using UnityEngine;\nusing UnityEngine.UI;\n")
    replace_once(
        inventory_layout,
        "    private float ResolveInventoryCellWidth(\n",
        """    internal static void ConfigureInventoryGrid(
        GridLayoutGroup grid,
        int columns,
        float cellWidth)
    {
        if (grid == null)
        {
            throw new ArgumentNullException(nameof(grid));
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns));
        }

        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.cellSize = new Vector2(cellWidth, InventoryCellHeight);
        grid.spacing = new Vector2(InventoryCellSpacing, InventoryCellSpacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = InventoryRows;
    }

    private float ResolveInventoryCellWidth(
""",
    )

    inventory_ui = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigGameHudCanvas.Inventory.cs"
    replace_once(
        inventory_ui,
        "            ResidentInventoryCompartment.Weapon,\n            \"WEAPON\",\n",
        "            ResidentInventoryCompartment.Weapon,\n            string.Empty,\n",
    )
    replace_once(
        inventory_ui,
        """        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.cellSize = new Vector2(cellWidth, InventoryCellHeight);
        grid.spacing = new Vector2(InventoryCellSpacing, InventoryCellSpacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
""",
        "        ConfigureInventoryGrid(grid, columns, cellWidth);\n",
    )

    supported = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.SupportedActionPositions.cs"
    replace_once(supported, "using Dig.Domain.Navigation;\n", "")
    replace_once(
        supported,
        "        return candidates.Distinct().OrderBy(value => value).ToArray();",
        "        return candidates.Distinct().ToArray();",
    )
    replace_regex_once(
        supported,
        r"\n    private bool IsSupportedStationaryActionPath\(.*?\n    internal bool HasFullStandingSupport",
        "\n    internal bool HasFullStandingSupport",
    )

    mushroom_navigation = "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.MushroomNavigation.cs"
    replace_once(
        mushroom_navigation,
        """        if (!path.Succeeded
            || path.Path == null
            || !HasFullStandingSupport(definition.WorkPosition)
            || !IsSupportedStationaryActionPath(navigation, path.Path))
""",
        """        if (!path.Succeeded
            || path.Path == null
            || !HasFullStandingSupport(definition.WorkPosition))
""",
    )
    replace_once(
        mushroom_navigation,
        """        foreach (CellId candidate in candidates
            .Where(navigation.IsWalkable)
            .Where(HasFullStandingSupport)
            .Distinct()
            .OrderBy(value => value))
""",
        """        foreach (CellId candidate in candidates
            .Where(navigation.IsWalkable)
            .Where(HasFullStandingSupport)
            .Distinct())
""",
    )
    replace_once(
        mushroom_navigation,
        """            if (!path.Succeeded
                || path.Path == null
                || !IsSupportedStationaryActionPath(navigation, path.Path))
""",
        """            if (!path.Succeeded
                || path.Path == null)
""",
    )

    quality = "tools/quality/unity_gameplay_hud_contracts.py"
    replace_once(
        quality,
        '            "constraintCount = columns",\n',
        '            "ConfigureInventoryGrid(grid, columns, cellWidth);",\n',
    )
    replace_once(
        quality,
        '            "InventoryCellHeight = 76f",\n',
        '            "InventoryCellHeight = 76f",\n            \'"WEAPON"\',\n            "GridLayoutGroup.Axis.Horizontal",\n            "GridLayoutGroup.Constraint.FixedColumnCount",\n            "constraintCount = columns",\n',
    )

    Path("tests/Dig.Tests/MushroomMovementPlannerSourceContractTests.cs").write_text(
        r'''using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class MushroomMovementPlannerSourceContractTests
{
    [Fact]
    public void Mushroom_travel_uses_normal_navigation_and_supports_only_final_work_cell()
    {
        string root = FindRepositoryRoot();
        string runtime = Path.Combine(root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Runtime");
        string playMode = Path.Combine(root, "unity", "Dig.Unity", "Assets", "Dig.Unity", "Tests", "PlayMode");
        string mushrooms = Read(runtime, "DigTerrainWorkSession.Mushrooms.cs");
        string navigation = Read(runtime, "DigTerrainWorkSession.MushroomNavigation.cs");
        string supported = Read(runtime, "DigTerrainWorkSession.SupportedActionPositions.cs");
        int declarations = 0;

        foreach (string path in Directory.GetFiles(runtime, "DigTerrainWorkSession*.cs"))
        {
            declarations += Count(Normalize(File.ReadAllText(path)), "boolTryPlanMushroomMovement(");
        }

        Assert.Equal(1, declarations);
        Assert.DoesNotContain("TryPlanMushroomMovement(", mushrooms);
        Assert.Contains("privateboolTryPlanMushroomMovement(", navigation);
        Assert.Contains("_routePlans[job.Id]=newTerrainWorkRoutePlan", navigation);
        Assert.Contains("GetSameHeightActionCandidates(target)", navigation);
        Assert.Contains(".Where(HasFullStandingSupport)", navigation);
        Assert.Contains("HasFullStandingSupport(definition.WorkPosition)", navigation);
        Assert.DoesNotContain("IsSupportedStationaryActionPath", navigation);
        Assert.DoesNotContain("IsSupportedStationaryActionPath", supported);
        Assert.DoesNotContain("path.Cells.Any", supported);
        Assert.Contains("returncandidates.Distinct().ToArray();", supported);
        Assert.DoesNotContain("target.Y-1", navigation);
        Assert.DoesNotContain("target.Y+1", navigation);

        string direct = File.ReadAllText(Path.Combine(playMode, "MushroomChoppingPlayModeTests.cs"));
        string automatic = File.ReadAllText(Path.Combine(playMode, "CampfireFoodWorkflowPlayModeTests.cs"));
        Assert.Contains("Direct_command_completes_large_mushroom_drops_and_same_cell_regrowth", direct);
        Assert.Contains("Missing_cap_creates_large_mushroom_dependency_then_world_supply", automatic);
    }

    private static string Read(string root, string file) => Normalize(
        File.ReadAllText(Path.Combine(root, file)));

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static string Normalize(string source) => source
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Replace("\t", string.Empty, StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);
}
}
''',
        encoding="utf-8",
    )

    Path("tests/Dig.Tests/ResidentInventoryGridSourceContractTests.cs").write_text(
        r'''using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class ResidentInventoryGridSourceContractTests
{
    [Fact]
    public void Inventory_hides_weapon_title_and_fills_two_rows_by_column()
    {
        string runtime = Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string inventory = File.ReadAllText(
            Path.Combine(runtime, "DigGameHudCanvas.Inventory.cs"));
        string layout = File.ReadAllText(
            Path.Combine(runtime, "DigGameHudCanvas.InventoryLayout.cs"));

        Assert.DoesNotContain("\"WEAPON\"", inventory);
        Assert.Contains(
            "ResidentInventoryCompartment.Weapon,\n            string.Empty,",
            inventory);
        Assert.Contains("ConfigureInventoryGrid(grid, columns, cellWidth);", inventory);
        Assert.Contains("grid.startAxis = GridLayoutGroup.Axis.Vertical;", layout);
        Assert.Contains(
            "grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;",
            layout);
        Assert.Contains("grid.constraintCount = InventoryRows;", layout);
        Assert.DoesNotContain("GridLayoutGroup.Axis.Horizontal", inventory + layout);
        Assert.DoesNotContain("GridLayoutGroup.Constraint.FixedColumnCount", inventory + layout);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
''',
        encoding="utf-8",
    )

    basket_test = "unity/Dig.Unity/Assets/Dig.Unity/Tests/PlayMode/BasketInventoryLifecyclePlayModeTests.cs"
    replace_once(
        basket_test,
        "using UnityEngine.TestTools;\n",
        "using UnityEngine.TestTools;\nusing UnityEngine.UI;\n",
    )
    replace_once(
        basket_test,
        """    [Test]
    public void Campfire_has_same_plane_side_candidates_for_every_orientation()
""",
        """    [Test]
    public void Inventory_grid_orders_slots_top_bottom_by_column()
    {
        _root = new GameObject("Inventory Grid Order Test", typeof(RectTransform));
        RectTransform root = (RectTransform)_root.transform;
        root.sizeDelta = new Vector2(300f, 100f);
        GridLayoutGroup grid = _root.AddComponent<GridLayoutGroup>();
        DigGameHudCanvas.ConfigureInventoryGrid(grid, columns: 3, cellWidth: 52f);
        RectTransform[] slots = Enumerable.Range(0, 6)
            .Select(index =>
            {
                GameObject slot = new GameObject($"Slot {index + 1}", typeof(RectTransform));
                RectTransform rect = (RectTransform)slot.transform;
                rect.SetParent(root, worldPositionStays: false);
                return rect;
            })
            .ToArray();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        Assert.That(grid.startAxis, Is.EqualTo(GridLayoutGroup.Axis.Vertical));
        Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
        Assert.That(grid.constraintCount, Is.EqualTo(2));
        Assert.That(slots[0].anchoredPosition.x,
            Is.EqualTo(slots[1].anchoredPosition.x).Within(0.01f));
        Assert.That(slots[0].anchoredPosition.y, Is.GreaterThan(slots[1].anchoredPosition.y));
        Assert.That(slots[2].anchoredPosition.x, Is.GreaterThan(slots[0].anchoredPosition.x));
        Assert.That(slots[2].anchoredPosition.y,
            Is.EqualTo(slots[0].anchoredPosition.y).Within(0.01f));
    }

    [Test]
    public void Campfire_has_same_plane_side_candidates_for_every_orientation()
""",
    )


def main() -> None:
    if len(sys.argv) != 2 or sys.argv[1] not in {"docs", "code"}:
        raise SystemExit("usage: apply_inventory_mushroom_regression.py docs|code")
    if sys.argv[1] == "docs":
        update_docs()
    else:
        update_code()


if __name__ == "__main__":
    main()
