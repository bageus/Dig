# Issues #92 and #109 — terrain output vertical slice

Status: `IMPLEMENTED` after merge; licensed Unity runtime evidence pending.

Authoritative design: [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md), [`../design/content/materials.md`](../design/content/materials.md), [`../design/material-demand-and-hauling.md`](../design/material-demand-and-hauling.md).

Tracking: #87, #92, #109; dependent planner #110.

## Delivered

- six typed terrain definitions with stable IDs and raw ore/material outputs;
- independent deterministic profile entry rolls, including empty and multi-output results;
- mutually exclusive terrain/deposit plans;
- Application-owned preflight and commit for World, deposit, Inventory, Job and exactly-once ledger;
- quantity-one world entities, deterministic derived entity IDs and identity-preserving storage hauling;
- mining-output ledger v2 and save format v13;
- legacy `material.metal -> material.iron` migration;
- catalog/output/ledger diagnostics;
- Domain, integration, save/migration, source-contract and checked-in Unity Play Mode regressions.

## Explicit boundary

Q-014 numeric probabilities, yields and work effort remain open. Existing numeric content values are preserved as versioned fixtures, not approved balance.

#110 remains responsible for active building demands, current fog eligibility, destination-demand reservations and their cancel/retry/save workflow. Explicit storage filters continue to be valid collection demand.

## Publication integrity

The implementation was rebased on `main` commit `7d3c54451428217cc0d143478e401f8a09547416`. Its 182091-byte patch was verified with SHA-256 `1f421e89d1cfa1ff0cac580b53bc55fd4613cd97e86f87499c7b262c13cbc044` before application. The resulting PR contains only the 58 documented source, test and design files; temporary transport payloads and workflows are absent from the implementation diff.

## CI compile correction

The first pre-rebase Release build exposed three test-only compatibility errors: the non-solid test material required the full constructor, and two empty-output calls needed an explicit empty `EntityId` collection after the new multi-output overload was introduced. All three calls bind explicitly in the rebased source without changing runtime behavior.
