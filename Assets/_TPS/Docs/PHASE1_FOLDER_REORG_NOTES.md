# Phase 1 Folder Reorg Notes

Phase 1 runtime ownership is already stable, but the current runtime folder map still clusters several non-combat domains under `Assets/_TPS/Scripts/Runtime/Combat`.

## Intended Domain Split

- `Runtime/Inventory`
- `Runtime/Economy`
- `Runtime/Party`
- `Runtime/Progression`
- `Runtime/Rewards`

## Current Limitation

The active Codex sandbox in this session allowed compile-safe code edits, installer updates, audit tooling, and docs work, but it blocked the final Unity-guid-safe move/delete pass for existing runtime files.

That means:

- the authoritative runtime files are still the existing Phase 1 files that compile and pass audit today,
- the target folders were scaffolded for the next safe move pass,
- the actual GUID-preserving file relocation should be done inside Unity or an unrestricted workspace pass so `.meta` moves stay intact and no duplicate GUID window is introduced.

## Why This Was Not Forced

Risk 2, Risk 5, and the playable smoke proof were higher priority than directory cosmetics. Forcing file relocation under the current filesystem restriction would have been more likely to create reference churn than to reduce risk.

## Safe Follow-Up

1. Move the authoritative runtime files from `Runtime/Combat` into the target domain folders using Unity or a workspace that permits move/delete operations on tracked files.
2. Preserve each existing `.meta` file during the move.
3. Re-run:
   - `Tools/TPS/Phase 1/Reinstall And Audit`
   - EditMode tests `TPS.Editor.Tests.Phase1EditModeTests`
   - the manual smoke flow in `PHASE1_MANUAL_SMOKE.md`
