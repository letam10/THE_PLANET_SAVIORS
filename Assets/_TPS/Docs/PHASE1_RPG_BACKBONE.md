# Phase 1 RPG Backbone

## Ownership
- `WorldClock` owns time.
- `WeatherSystem` owns weather.
- `QuestService` owns raw quest progress.
- `DialogueStateService` owns raw dialogue flags, selected choices, and consumed one-shots.
- `PartyService` owns party roster, active slots, equipped weapon ids, and carried combat resources.
- `InventoryService` owns item/equipment stacks.
- `ProgressionService` owns levels, EXP, and unlocked skills.
- `EncounterService` owns encounter clear state and pending encounter context.
- `ZoneStateService` owns only durable zone facts.
- `EconomyService` owns currency, shop unlocks, stock, and restock markers.

## Derived State
- `StateResolver` recomputes derived state from owner systems.
- Derived state includes NPC visibility/location, dialogue variant selection, encounter table selection, ambient toggles, and dotted mirror keys in `GameStateManager`.
- `encounter_table_id` is derived and is not persisted as raw zone state.

## Save Policy
- Save schema is locked to `SaveVersion = 2`.
- Save data serializes owner state only.
- Time and weather are serialized from `WorldClock` and `WeatherSystem`; no duplicate owner exists in save data.
- Unsupported save versions are rejected before runtime state mutation.
- Resource state is clamped against the latest computed max values after `load`, `equip/unequip`, `level up`, and passive/unlock changes. `CurrentHP` and `CurrentMP` must never exceed recomputed max or fall below zero.

## Vertical Slice Wiring
- `Bootstrap` remains the main entry scene through the Windows Build Profile.
- `Core` hosts the Phase 1 services and resolver.
- `ZN_Town_AsterHarbor` contains the NPC schedule/dialogue anchors, merchant, inn, patrol encounter anchor, and sub-boss anchor.
- `BTL_Standard` hosts `BattleWorldBridge` for world-to-battle reward and consequence application.

## Verification Gates
- Project compiles clean.
- `Tools/TPS/Install Phase1 Vertical Slice` seeds data and scene wiring idempotently.
- EditMode suite `TPS.Editor.Tests.Phase1EditModeTests` covers overnight schedule, wet/lightning-vs-fire combat math, progression growth, economy buy/sell, and save schema version.

## Catalog Note
- `Phase1ContentCatalog` is still the shared content registry for the current branch.
- It is treated as a temporary neutral runtime catalog despite the legacy name.
- Gameplay ownership does not live in the catalog; it remains in the owner services.
