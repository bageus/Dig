# Campfire physical placement policy

## Ownership

The existing `BuildingPlacementValidator` remains the owner of ordinary logical building placement against World, occupied building cells and Navigation reachability.

`PackableBuildingPlacementPolicyValidator` adds the physical surface gate required by packable content. It consumes immutable surface facts and never reads Unity objects or mutates World, Buildings, Inventory or Jobs.

The Application placement preview and confirmation paths must invoke both validators with the same origin and current facts. Unity only renders the returned validity and reason code.

## Campfire footprint

The campfire content declares a physical square of `1.5 x 1.5` cells. Logical occupancy is conservative: each physical dimension is rounded up, so the current square covers a `2 x 2` logical-cell set anchored at the selected origin.

This prevents another building from occupying the partially covered edge area. The exact physical dimensions remain available for the placement silhouette and final visual scale.

## Surface facts

Each covered logical cell supplies:

- the exact XYZ cell;
- surface elevation;
- `OutdoorGround` or `Tunnel` classification.

Missing facts fail closed. A campfire succeeds only when all four covered cells are known, outdoor, equal in elevation and unoccupied.

## Stable failures

The policy returns stable Domain errors for:

- missing surface coverage;
- forbidden tunnel placement;
- a non-flat footprint;
- physical-footprint overlap.

## Next integration slice

The next #332 slice maps authoritative World/terrain presentation facts into these surface records and calls the policy from both `PreviewBuildingBoxPlacement` and `ConfirmBuildingBoxPlacementHandler`. The same result must drive ghost color and final command acceptance.