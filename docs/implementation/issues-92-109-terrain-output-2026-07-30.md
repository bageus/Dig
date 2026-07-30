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
- mining-output ledger v2 and save format v12;
- legacy `material.metal -> material.iron` migration;
- catalog/output/ledger diagnostics;
- Domain, integration, save/migration, source-contract and checked-in Unity Play Mode regressions.

## Explicit boundary

Q-014 numeric probabilities, yields and work effort remain open. Existing numeric content values are preserved as versioned fixtures, not approved balance.

#110 remains responsible for active building demands, current fog eligibility, destination-demand reservations and their cancel/retry/save workflow. Explicit storage filters continue to be valid collection demand.

## Publication integrity

The prepared 181871-byte implementation patch was verified by SHA-256 before application. The resulting PR contains only the 55 documented source, test and design files; temporary transport payloads and workflows are not part of the implementation diff.
