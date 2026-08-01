# PR #553 unified item interaction validation — 2026-08-01

Status: `IMPLEMENTED`; licensed Unity runtime evidence remains required before `VERIFIED`.

Authoritative design: [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md).
Implementation note: [`unified-item-interaction-capabilities-2026-08-01.md`](unified-item-interaction-capabilities-2026-08-01.md).
Tracking PR: [#553](https://github.com/bageus/Dig/pull/553).
Related issues: [#67](https://github.com/bageus/Dig/issues/67), [#70](https://github.com/bageus/Dig/issues/70), [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390), [#459](https://github.com/bageus/Dig/issues/459), [#511](https://github.com/bageus/Dig/issues/511).

## Finalized implementation scope

- `ItemDefinition.Interactions` is the authoritative capability profile for world pickup/use, inventory placement/use and exact-stack quick drop.
- World hover and click resolve one exact stack, action and availability result.
- Inventory hover and click use the live slot profile and exact `StackId`.
- Input priority is `Alt use -> C quick drop -> primary placement`.
- BuildingBox and consumables use explicit data profiles; ordinary materials, tools and weapons inherit generic interaction behavior without Unity item-ID branches.
- Rejected item actions consume the pointer and cannot fall through to ground movement or excavation.
- Legacy item-prefix classifiers and the world interaction override dictionary are removed.

## Executed validation

Validation run `30719083090` executed the reconstructed feature over the then-current `main` before publishing the clean source commit.

The final clean PR head was validated by Quality run `30719411589`; both Stage 2 source exports also passed (`30719411609` v2 and `30719411594` v3).

Executed checks:

- architecture, file-size, C# compatibility and Unity source-contract gates;
- Release restore and build;
- full .NET test suite;
- headless smoke;
- standard deterministic soak;
- large-settlement deterministic soak.

The clean feature branch contains no `.agent` payload files, temporary publishing workflows or generated soak report files.

## Runtime verification boundary

Checked-in Play Mode coverage includes generic material, weapon, food and BuildingBox interaction workflows. Unity workflow `30719411577` completed through the blocked-evidence path: licensed activation was unavailable, so actual EditMode/PlayMode execution was skipped. The system therefore remains `IMPLEMENTED`, not `VERIFIED`.
