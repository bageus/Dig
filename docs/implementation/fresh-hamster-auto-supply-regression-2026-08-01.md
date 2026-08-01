# Fresh hamster auto-supply regression — 2026-08-01

Status: `IMPLEMENTED` on branch `bugfix/fresh-hamsters-not-auto-supplied`; repository Quality and licensed Unity runtime evidence are pending.

Authoritative specifications:

- [`../design/hamsters-and-grubs-ecology.md`](../design/hamsters-and-grubs-ecology.md);
- [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).

Tracking issues: [#433](https://github.com/bageus/Dig/issues/433), [#524](https://github.com/bageus/Dig/issues/524).

## Reported symptom

Immediately after fresh startup, the selected dwarf inventory showed two `Hamster` slots with `R:1`. The animals were no longer free in the world even though fresh bootstrap had created two world-owned hamster entities.

## Root cause

The `R:1` marker is a supply reservation. `CampfireProductionContent.CreateWorkstation()` configured the hamster internal-stock rule as:

```csharp
new InternalStockRuleDefinition(HamsterItemId, 2, true, 100)
```

The third argument enabled delivery by default. After PR #547 restored continuous refill, the first eligible supply passes could reserve the two fresh world hamster and move them through resident inventory toward campfire stock. The seed planner and `ItemLocation.InWorld` commit were not the final owner-transition defect.

## Correction

The hamster rule keeps capacity `2`, priority `100`, recipe support, manual toggle and ordinary supply behavior after toggle-on, but starts disabled:

```csharp
new InternalStockRuleDefinition(HamsterItemId, 2, false, 100)
```

Mushroom cap, mushroom leg and stone defaults remain enabled. No new runtime branch or living-material-specific supply service was added; the existing data-driven stock toggle remains the single owner.

## Regression coverage

- `CampfireProductionContentTests` requires hamster capacity `2` with `DefaultDeliveryEnabled == false` and requires every non-hamster campfire stock default to remain enabled.
- `LivingMaterialUnityRuntimeContractTests` requires the opt-in content declaration and the checked-in Unity scenario.
- `LivingMaterialEcologyPlayModeTests` initializes the real demo building, production and living-material sessions, runs initial production synchronization, then requires:
  - two world hamster;
  - zero hamster reserved quantity;
  - hamster stock delivery disabled;
  - no non-terminal `BuildingSupplyJobDefinition` requesting hamster;
  - no hamster/grub in any resident inventory layout.

## Verification boundary

The .NET/domain/source-contract suite can verify the content owner and checked-in scenario wiring. Runtime status remains `IMPLEMENTED`, not `VERIFIED`, until a licensed Unity runner executes the Play Mode scenario and records machine-readable evidence.
