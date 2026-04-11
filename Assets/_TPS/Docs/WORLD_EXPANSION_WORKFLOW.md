# World Expansion Workflow

## What This Pass Generates
- A larger `ZN_Town_AsterHarbor` with readable districts, extra travel landmarks, and expanded placeholder dressing.
- Two settlement scenes:
  - `ZN_Settlement_Gullwatch`
  - `ZN_Settlement_RedCedar`
- Two dungeon scenes:
  - `DG_TideCaverns`
  - `DG_QuarryRuins`
- A rebuilt placeholder battle arena in `BTL_Standard`.

## Rebuild Menus
- Full rebuild:
  - `Tools/TPS/World/Install Expanded Playable World`
- Main hub only:
  - `Tools/TPS/World/Rebuild Expanded AsterHarbor`
- Settlements only:
  - `Tools/TPS/World/Rebuild Settlements`
- Dungeons only:
  - `Tools/TPS/Dungeon/Rebuild Dungeon Scaffolds`
- Battle scene only:
  - `Tools/TPS/Battle/Rebuild Standard Arena`
- Validation:
  - `Tools/TPS/World/Validate Expanded Layout`

## Replace-Safe Scene Rules
- Generated content lives under managed roots and can be rebuilt safely.
- Keep slot roots with `EnvironmentGeneratedMarker`.
- Keep all `SceneTravelAnchor`, `SpawnPoint`, `DialogueAnchor`, `EncounterAnchor`, `MerchantAnchor`, `InnAnchor`, and `NPCSchedule` hooks.
- Replace visuals by adding your art as custom children under the slot root.
- Do not treat `GEN_*` children as final art storage if you still want to rerun generators.

## Safe To Replace
- Placeholder buildings, roofs, signs, docks, piers, fences, lamps, crates, shrubs, rocks, trees, ambient markers, and battle props inside generated roots.

## Must Keep
- Travel anchors between hub, settlements, and dungeons.
- Spawn IDs:
  - `Default`
  - `GullwatchDock`
  - `RedCedarRoad`
  - `TideCavernsGate`
  - `QuarryRuinsGate`
- Existing quest, dialogue, shop, inn, encounter, and smoke/test hooks.

## Validation After Art Changes
1. `Tools/TPS/Content/Run Content Validation`
2. `Tools/TPS/World/Validate Expanded Layout`
3. `Tools/TPS/Phase 1/Run Project Audit`
4. If you changed the main traversal route, run `Tools/TPS/Functional Lock/Prepare Final Core Smoke`

## Manual Playtest Focus
- `Tab` toggles `UI Mode` and `Gameplay Mode`.
- In `UI Mode`, use the mouse to test save, load, inventory, equipment, and quest panels.
- Walk the route:
  - `AsterHarbor -> Gullwatch -> AsterHarbor`
  - `AsterHarbor -> RedCedar -> AsterHarbor`
  - `AsterHarbor -> TideCaverns`
  - `AsterHarbor -> QuarryRuins`
- Confirm travel returns to the expected spawn marker and does not block key gameplay hooks.
