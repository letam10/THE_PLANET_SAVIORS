# Phase 2 + 3 Runtime And Content Pipeline

## Runtime hardening
- `ProgressionService` now computes party-facing stats through `CharacterStatSnapshot` and `ComputedStats`.
- Battle, HUD, equipment flow, and consumable/resource handling are expected to read from the snapshot path instead of rebuilding ad-hoc stat totals.
- Passive progression remains thin: archetype unlocks can now grant a skill and a passive stat modifier at the same unlock gate.
- `PartyService` clamps `CurrentHP` and `CurrentMP` after:
  - `load`
  - `equip`
  - `unequip`
  - `level up`
  - passive/unlock refresh
- Equipment ownership remains inventory-count based. Equipped copies must be backed by owned copies, and selling cannot reduce ownership below equipped usage.

## Authoring workflow
- Primary menu paths:
  - `Tools/TPS/Content/Install Or Update Aster Harbor Proof Content`
  - `Tools/TPS/Content/Install And Audit Proof Content`
  - `Tools/TPS/Content/Run Content Validation`
  - `Tools/TPS/Phase 1/Run Project Audit`
  - `Tools/TPS/Phase 1/Prepare Manual Smoke`
- Preferred workflow for new bounded content on this branch:
  1. Add or update definitions in `Assets/_TPS/Data/Phase1`.
  2. Extend installer/seeder instead of hand-wiring scene references when the same result can be produced through tooling.
  3. Run content validation.
  4. Reinstall and audit.
  5. Verify in `Bootstrap` with the runtime HUD / smoke panel.

## Bounded proof content
- Phase 3 proof content is intentionally scoped to one optional side quest in `AsterHarbor`.
- Current proof bundle contains:
  - `1` quest definition
  - `1` reward table
  - `1` optional encounter
  - `1` new dialogue bundle with `3` variants
  - `1` new NPC anchor in town
  - `1` visible state consequence in the world (`dock_supplies_secured`)
- Out of scope for this branch:
  - new zone
  - new merchant system
  - large progression branch expansion
  - automation-bridge deep work

## Validation and QA
- Fast verification:
  - recompile scripts
  - `Tools/TPS/Content/Run Content Validation`
  - `Tools/TPS/Phase 1/Run Project Audit`
- Strong verification:
  - `Tools/TPS/Content/Install And Audit Proof Content`
  - EditMode tests
  - `Tools/TPS/Phase 1/Prepare Manual Smoke`
  - press Play from `Bootstrap`
- Manual checks for the Phase 3 proof content:
  1. Talk to `Quartermaster Ivo`.
  2. Accept `Secure Dock Supplies`.
  3. Trigger `ENC_DockRainMites_Anchor`.
  4. Win the battle and return to world.
  5. Confirm reward, quest state change, and dialogue change.
  6. Turn in the quest with `Quartermaster Ivo`.
  7. Confirm the dock supply banner becomes visible and stays correct after save/load.

## Known limitations
- Full 13-step end-to-end automation remains parked. Manual smoke is still the truthful proof path for complete world-to-battle-to-world behavior.
- `Phase1ContentCatalog` keeps its legacy name for now. Treat it as the current shared registry, not as a Phase-1-only concept.

## Functional lock references
- See `Assets/_TPS/Docs/FEATURE_LOCK_MATRIX.md` for the locked vs deferred system decision table.
- See `Assets/_TPS/Docs/GAMEPLAY_CONTRACTS.md` for the production content/runtime contracts.
- See `Assets/_TPS/Docs/FINAL_VERIFICATION_PACK.md` for the final verification flow.
- See `Assets/_TPS/Docs/ART_INTEGRATION_CONTRACT.md` for safe art replacement rules.
- See `Assets/_TPS/Docs/FUNCTIONAL_LOCK_DECISION.md` for the final branch-level lock policy.
