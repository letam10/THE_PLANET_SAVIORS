# THE_PLANET_SAVIORS — Codex workflow

## 1. Mandatory Workflow & Planning
- For any non-trivial task, start by producing a plan as a markdown table.
- The plan must include: Goal, Files to change, Unity Assets to modify (Prefabs/Scenes), Risks, Validation steps, Commit message.
- After producing the plan, stop and wait for explicit user approval.
- Always work on the current worktree branch, never on `main`. Prefer the branch pattern: `codex/<task-name>`.
- Break work into small checkpoints (roughly 1 to 5 related files or one completed subtask).
- After each checkpoint:
  1. Review the current git diff.
  2. Stage the files for that checkpoint. **CRITICAL: Always stage the corresponding `.meta` files alongside any modified, added, or deleted assets/scripts.**
  3. Create one checkpoint commit.
  4. Continue to the next checkpoint.

## 2. Unity & Architecture Execution Rules (TPS Specific)
- **Folder Structure:** Keep all gameplay/runtime code strictly under `Assets/_TPS/Scripts/...` and content under `Assets/_TPS/Data/...`. Do not edit `Library`, `Temp`, `Logs`, or `PackageCache`.
- **Namespaces:** Always use the `TPS.[Module]` namespace convention (e.g., `TPS.Runtime.Combat`, `TPS.Runtime.Core`).
- **Data-Driven Constraints:** 
  - **ScriptableObjects (SOs)** are templates (Read-only at runtime). **NEVER** write runtime state changes into SOs.
  - **Runtime State** must be stored in POCO classes (e.g., `SaveData` instances) and managed via `GameStateManager` or specific runtime controllers.
- **Event Communication:** Use `GameEventBus` ONLY for cross-domain/macro events (e.g., `OnHourChanged`, `OnWeatherChanged`). Use direct references (`[SerializeField]`) or C# actions for local/internal mechanics.
- **Tooling:** Use Unity MCP tools for Scene, GameObject, Component, and Editor operations whenever possible instead of manual file editing.

## 3. Commit Policy
- Do not wait until the very end to make a single giant commit.
- Prefer small conventional commits.
- Suggested prefixes: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`, `ui:`.
- If a diff is too broad, split it before committing.
- **NEVER** commit scenes (`.unity`) or prefabs (`.prefab`) if the task only required code changes, to avoid accidental serialization conflicts.

## 4. Validation Before Commit
- If code changed, run the smallest relevant validation before each checkpoint commit (e.g., check for compiler errors).
- For Unity tasks, prefer narrow validation first, not a full project rebuild.
- **Unity specific checks:**
  - Ensure no new compiler errors or warnings in the Unity Console.
  - Ensure no broken serialized references (`Missing (Mono Script)` or `Null Reference`) in touched scenes/prefabs.
- If validation fails, fix the issue first or stop and report the failure clearly before committing.
- Do not merge to `main`. Do not create a PR unless explicitly asked.

## 5. Planning Table Format
| Section | Content |
|---|---|
| Goal | ... |
| Files & Assets to change | (Include .cs, .prefab, .asset, and .meta files) |
| Risks | (e.g., Save/Load compatibility, Prefab overrides, Event coupling) |
| Validation | (e.g., "Compile without errors, check Inspector references") |
| Commit message | ... |

## Mandatory workflow
- For any non-trivial task, start by producing a plan as a markdown table.
- The plan must include: Goal, Files to change, Risks, Validation steps, Commit message.
- After producing the plan, stop and wait for explicit user approval.
- Always work on the current worktree branch, never on main.
- Break work into small checkpoints.
- A checkpoint should usually be one coherent step, roughly 1 to 5 related files or one completed subtask.
- After each checkpoint:
  1. review the current git diff
  2. stage only the files for that checkpoint
  3. create one checkpoint commit
  4. continue to the next checkpoint

## Commit policy
- Do not wait until the very end to make a single giant commit.
- Prefer small conventional commits.
- Suggested prefixes: feat:, fix:, refactor:, docs:, test:, chore:
- If a diff is too broad, split it before committing.

## Validation before each commit
- If code changed, run the smallest relevant validation before each checkpoint commit.
- For Unity tasks, prefer narrow validation first, not full project rebuild every time.
- If validation fails, fix or revert before committing.

## Execution rules after approval
- Work only on the current worktree / branch.
- Never touch main directly.
- Prefer the branch pattern: codex/<task-name>.
- Use Unity MCP tools for scene, GameObject, component, and editor operations whenever possible.
- Keep all gameplay/runtime code under Assets/_TPS.
- Do not edit Library, Temp, Logs, or PackageCache manually unless explicitly required.

## Validation before commit
- Run the project validation steps before committing.
- If the task changes code, run build/test/lint commands that are available.
- If validation fails, fix the issue first or stop and report the failure clearly.

## Commit rules
- Only commit after validation passes.
- Use one clear commit message in conventional style.
- Do not merge to main.
- Do not create a PR unless the user explicitly asks.

## Planning table format
| Section | Content |
|---|---|
| Goal | ... |
| Files to change | ... |
| Risks | ... |
| Validation | ... |
| Commit message | ... |