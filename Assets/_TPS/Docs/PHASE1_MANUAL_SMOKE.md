# Phase 1 Manual Smoke

Use this when Play Mode cannot be fully automated from tooling. The goal is to prove the Phase 1 vertical slice works end-to-end from boot through battle consequence, zone recompute, and save/load restore.

## Prep

1. Run `Tools/TPS/Phase 1/Prepare Manual Smoke`.
2. Optional but recommended: run `Tools/TPS/Phase 1/Run Project Audit` first and confirm it passes cleanly.
3. Open the Game view and keep the `Phase 1 Smoke` panel visible during the run.

## Expected Proof Points

The smoke run is successful only if all of these can be observed:

1. Boot starts from `Bootstrap`, then the content flow reaches `Core`, then `ZN_Town_AsterHarbor`.
2. The smoke panel shows the current quest, dialogue variant, NPC location, encounter table, boss clear state, party count, currency, and save presence.
3. The harbor NPC stands in the square between `07:00` and `12:00`.
4. Switching weather to `Rain` moves that NPC indoors to the tavern schedule slot.
5. Accepting the harbor quest changes quest state and dialogue state.
6. Entering the gate-side boss encounter loads `BTL_Standard`.
7. Clearing the sub-boss grants visible rewards and returns to world.
8. The world mirrors change after battle: boss clear true, dialogue variant changes, and the zone encounter table swaps.
9. Sleeping advances the day and refreshes shop stock / zone-derived state.
10. Saving creates a save baseline; loading restores the same smoke-critical state without mismatch.

## Step-by-Step Checklist

1. Press Play from `Bootstrap`.
Expected:
`Phase 1 Smoke` timeline logs the scene flow and eventually shows `Scene: ZN_Town_AsterHarbor`.

2. In town, check the smoke panel immediately.
Expected:
Quest is `NotStarted`, dialogue is `start`, encounter table is the pre-boss table, boss cleared is `False`.

3. Confirm the captain NPC is in the square during `07:00-12:00`.
Expected:
Smoke panel shows `NPC: Visible @ MK_Square_01`.
Failure point:
NPC hidden, standing at tavern in sunny weather, or mirror key not updating.

4. Press the HUD `Rain` button.
Expected:
Smoke timeline logs weather change and the NPC moves to the tavern slot. Panel should show `NPC: Visible @ MK_Tavern_01`.
Failure point:
Weather changes but NPC location mirror stays stale.

5. Talk to Captain Rhea to accept the quest.
Expected:
Quest changes to `Active`, dialogue changes to `active`, and the smoke timeline logs both transitions.

6. Go to the gate-side sub-boss trigger and enter the encounter.
Expected:
Timeline logs the encounter and scene flow switches to `BTL_Standard`.

7. Win the battle.
Expected:
Reward feedback is visible in the runtime HUD/timeline, then the game returns to `ZN_Town_AsterHarbor`.

8. After returning to town, inspect the smoke panel.
Expected:
`Boss Cleared: True`, dialogue changes to `turn_in` or `completed` depending on whether the quest was turned in, and the encounter table changes to the post-boss table.
Failure point:
Battle reward happens but world-derived mirrors do not change.

9. Talk to Captain Rhea again to turn in the quest if needed.
Expected:
Quest becomes `Completed`, dialogue becomes `completed`, and party count increases when Lina joins.

10. Open the merchant and confirm stock / currency loop.
Expected:
You can buy or sell at least one item, and currency changes are reflected in the smoke panel.

11. Interact with the inn to sleep until next day.
Expected:
Timeline logs sleep advance, day increments, shop stock refreshes, and town-derived state remains coherent.

12. Press `Save`.
Expected:
Smoke timeline logs a save snapshot baseline and `Save Exists: True`.

13. Press `Load`.
Expected:
Smoke timeline reports `Load restored smoke-critical state correctly.`
Failure point:
Any `Load mismatch detected` message means save/load needs investigation before Phase 1 is considered closed.

## After Reload / Reinstall

Run this sequence after each risky editor action:

1. Reopen the project or trigger a script recompile.
2. Run `Tools/TPS/Phase 1/Reinstall And Audit`.
3. Run the manual smoke again from `Prepare Manual Smoke`.

This is the intended proof path for the current environment because full Play Mode automation is not available from the active MCP session.
