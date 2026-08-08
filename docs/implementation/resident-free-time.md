# Resident free time implementation

Authoritative designs:

- [`../design/leisure-variety-and-selection.md`](../design/leisure-variety-and-selection.md), tracking issue #143;
- [`../design/partnership-pregnancy-and-birth.md`](../design/partnership-pregnancy-and-birth.md), tracking issue #145.

Status: `IMPLEMENTED`; licensed Unity Play Mode verification remains pending.

## Ownership

- `AgentState` owns the active leisure choice, participant reference, next effect tick,
  repetition multiplier and the last-ten activity history.
- `SocietyState` owns partnerships, pregnancy, birth and postpartum cooldown.
- Unity selects reachable movement targets and renders movement, but it does not own Mood,
  leisure history, pregnancy or cooldown.

## Determinism and persistence

`LeisureActivitySelector` applies the approved novelty weight to stable-id ordered candidates
and uses world seed plus decision id. The selected activity starts before its first interval;
history is committed only with the first Mood effect. Agent runtime save data stores active
choice, partner, next interval, history and multiplier, so load neither rerolls nor repeats the
first effect.

## Verification

Domain tests execute direct-order penalties, interval/history semantics, repetition penalty,
deterministic weighted selection and runtime snapshot restoration. Static architecture and
C# 9 checks pass. Unity Play Mode evidence is still required before status can become
`VERIFIED`.
