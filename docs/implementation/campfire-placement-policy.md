# Campfire physical placement policy

## Ownership

The existing `BuildingPlacementValidator` remains the owner of ordinary logical building placement against World, occupied building cells and Navigation reachability.

`PackableBuildingPlacementPolicyValidator` adds the physical surface gate required by packable content. It consumes immutable surface facts and never reads Unity objects or mutates World, Buildings, Inventory or Jobs.

The Application placement preview and confirmation paths invoke both validators with the same origin and current facts. Unity only renders the returned visibility, validity and reason code.

## Campfire footprint

Campfire сохраняет визуальный размер `1.5 x 1.5` клетки, но authoritative logical occupancy для BuildingBox placement равен одной anchor cell. В side-view layered world округление визуального размера до `2 x 2` логических клеток ошибочно превращало нижнюю клетку в solid support и скрывало ghost на Z1–Z3.

Placement profile требует ровную опору, но разрешает как outdoor, так и tunnel surface. Поэтому supported anchor cell на Z1, Z2 или Z3 показывает building ghost и может быть подтверждена; Z0 остаётся отдельным relocation intent.

## Surface facts

Surface facts are projected from authoritative World cells rather than inferred from the placement layer:

- every occupied footprint cell must be open and explored;
- the lower occupied cell of every horizontal/depth footprint column must have an explored solid terrain cell immediately below it at `Y + 1`;
- the support elevation is the actual Y coordinate of that solid cell;
- every required support column must resolve to the same elevation for a flat-surface building;
- surface classification remains `OutdoorGround` or `Tunnel`.

Missing support fails closed. Interactive placement hides the ghost over unsupported air, and authoritative confirmation repeats the same support projection before creating a plan/job. Campfire succeeds when its single logical anchor cell is known, supported, equal to its only support elevation and unoccupied by another building/plan.

## Stable failures

The policy returns stable Domain errors for:

- missing terrain support or surface coverage;
- forbidden tunnel placement;
- a non-flat footprint;
- physical-footprint overlap.

## Validation

Unit coverage verifies direct support, missing-column rejection, Z0 BuildingBox support and preview/confirmation parity. Runtime source contracts require the ghost renderer to clear invisible unsupported previews and require confirmation/relocation handlers to revalidate support independently of Unity presentation.
