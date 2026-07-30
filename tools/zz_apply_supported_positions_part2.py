from pathlib import Path


def replace_required(path: str, old: str, new: str) -> None:
    p = Path(path)
    text = p.read_text()
    if old not in text:
        raise SystemExit(f"Required text not found in {path}: {old[:120]!r}")
    p.write_text(text.replace(old, new, 1))


def append_once(path: str, marker: str, content: str) -> None:
    p = Path(path)
    text = p.read_text()
    if marker not in text:
        p.write_text(text.rstrip() + "\n\n" + content.strip() + "\n")

Path("tests/Dig.Tests/FixedResidentStandingSupportQuery.cs").write_text(r"""using Dig.Application.Agents;
using Dig.Domain.World;

namespace Dig.Tests
{
internal sealed class FixedResidentStandingSupportQuery
    : IResidentStandingSupportQuery
{
    private readonly bool _supported;

    internal FixedResidentStandingSupportQuery(bool supported)
    {
        _supported = supported;
    }

    public bool HasFullStandingSupport(CellId cell) => _supported;
}
}
""")
replace_required(
    "tests/Dig.Tests/CampfireFoodSaveTests.cs",
    """            agents,
            new InMemoryInventoryRepository(inventory),
            new InMemoryExecutionJournal()).Handle(
""",
    """            agents,
            new InMemoryInventoryRepository(inventory),
            new FixedResidentStandingSupportQuery(supported: true),
            new InMemoryExecutionJournal()).Handle(
""")
foodtests = "tests/Dig.Tests/ResidentFoodMealTests.cs"
replace_required(
    foodtests,
    """        [Fact]
        public void Unsupported_carried_item_is_not_consumed()
""",
    """        [Fact]
        public void Unsupported_standing_position_does_not_consume_or_start_meal()
        {
            Harness harness = new Harness(
                foodQuantity: 1,
                nutrition: 1_000,
                supported: false);

            Result result = harness.Start(10);

            Assert.True(result.IsFailure);
            Assert.Equal(
                ResidentFoodMealErrors.UnsupportedStandingPosition,
                result.Error);
            Assert.Equal(1, harness.Inventory.GetStack(harness.StackId)!.Quantity);
            Assert.False(harness.Agent.HasActiveFoodMeal);
        }

        [Fact]
        public void Unsupported_carried_item_is_not_consumed()
""")
replace_required(
    foodtests,
    """            internal Harness(int foodQuantity, int nutrition, bool useFood = true)
""",
    """            internal Harness(
                int foodQuantity,
                int nutrition,
                bool useFood = true,
                bool supported = true)
""")
replace_required(
    foodtests,
    """                Handler = new StartResidentFoodMealHandler(
                    Agents,
                    InventoryRepository,
                    Journal);
""",
    """                Handler = new StartResidentFoodMealHandler(
                    Agents,
                    InventoryRepository,
                    new FixedResidentStandingSupportQuery(supported),
                    Journal);
""")

contract = "tests/Dig.Tests/MushroomMovementPlannerSourceContractTests.cs"
replace_required(
    contract,
    """        Assert.Contains("_routePlans[job.Id]=newTerrainWorkRoutePlan", navigation);
""",
    """        Assert.Contains("_routePlans[job.Id]=newTerrainWorkRoutePlan", navigation);
        Assert.Contains("GetSameHeightActionCandidates(target)", navigation);
        Assert.Contains(".Where(HasFullStandingSupport)", navigation);
        Assert.Contains("IsSupportedStationaryActionPath(navigation,path.Path)", navigation);
        Assert.DoesNotContain("target.Y-1", navigation);
        Assert.DoesNotContain("target.Y+1", navigation);
""")

play = "unity/Dig.Unity/Assets/Dig.Unity/Tests/PlayMode/MushroomChoppingPlayModeTests.cs"
replace_required(play, "using Dig.Presentation.Inventory;\n", "using Dig.Presentation.Inventory;\nusing Dig.Presentation.World;\n")
test_block = r"""
    [Test]
    public void Work_position_uses_supported_depth_cell_when_side_cells_are_void()
    {
        Assembly runtime = typeof(DigWorldInteraction).Assembly;
        object world = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigWorldSession"),
            "CreateDemo",
            20,
            14,
            5);
        WorldViewModel worldView = (WorldViewModel)Invoke(world, "LoadView");
        object journal = GetProperty(world, "Journal");
        object tunnel = Invoke(world, "CreateTunnelNavigationVolume");
        object residents = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigAgentSession"),
            "CreateDemo",
            worldView,
            tunnel,
            journal);
        AgentViewModel worker = ((IEnumerable)Invoke(residents, "LoadView"))
            .Cast<AgentViewModel>()
            .First();
        object terrain = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigTerrainWorkSession"),
            "CreateDemo",
            world,
            new[] { worker },
            journal,
            GetProperty(residents, "SkillGrants"));

        Dictionary<CellId, WorldCellViewModel> cells = worldView.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(value => new CellId(value.X, value.Y, value.Z));
        MethodInfo resolver = terrain.GetType().GetMethod(
            "TryResolveMushroomWorkPosition",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        CellId workerCell = new CellId(worker.CellX, worker.CellY, worker.CellZ);
        CellId? chosenTarget = null;
        CellId chosenWork = default;
        foreach (CellId target in cells.Values
            .Where(value => !value.IsSolid)
            .Select(value => new CellId(value.X, value.Y, value.Z))
            .OrderBy(value => value))
        {
            bool sideSupported =
                HasFullSupport(cells, new CellId(target.X - 1, target.Y, target.Z))
                || HasFullSupport(cells, new CellId(target.X + 1, target.Y, target.Z));
            bool depthSupported =
                (target.Z > CellId.MinimumDepth
                    && HasFullSupport(
                        cells,
                        new CellId(target.X, target.Y, target.Z - 1)))
                || (target.Z < CellId.MaximumDepth
                    && HasFullSupport(
                        cells,
                        new CellId(target.X, target.Y, target.Z + 1)));
            if (sideSupported || !depthSupported)
            {
                continue;
            }

            object[] arguments = { target, workerCell, default(CellId) };
            if (!(bool)resolver.Invoke(terrain, arguments)!)
            {
                continue;
            }

            CellId work = (CellId)arguments[2];
            if (work.X == target.X
                && work.Y == target.Y
                && Math.Abs(work.Z - target.Z) == 1
                && HasFullSupport(cells, work))
            {
                chosenTarget = target;
                chosenWork = work;
                break;
            }
        }

        Assert.That(
            chosenTarget.HasValue,
            Is.True,
            "The demo must expose a side-void/depth-supported action-position case.");
        Assert.That(chosenWork.Y, Is.EqualTo(chosenTarget!.Value.Y));
        Assert.That(chosenWork.X, Is.EqualTo(chosenTarget.Value.X));
        Assert.That(Math.Abs(chosenWork.Z - chosenTarget.Value.Z), Is.EqualTo(1));
        Assert.That(HasFullSupport(cells, chosenWork), Is.True);
    }

"""
replace_required(
    play,
    """    [Test]
    public void Renderer_places_large_mushroom_upright_slightly_above_resident_and_highlights_hover()
""",
    test_block + """    [Test]
    public void Renderer_places_large_mushroom_upright_slightly_above_resident_and_highlights_hover()
""")
helper = r"""
    private static bool HasFullSupport(
        IReadOnlyDictionary<CellId, WorldCellViewModel> cells,
        CellId actionCell)
    {
        CellId support = new CellId(
            actionCell.X,
            actionCell.Y + 1,
            actionCell.Z);
        return cells.TryGetValue(support, out WorldCellViewModel? value)
            && value.HasFullActorSupport;
    }

"""
replace_required(play, """    private static MushroomSiteSnapshot Snapshot(
""", helper + """    private static MushroomSiteSnapshot Snapshot(
""")
