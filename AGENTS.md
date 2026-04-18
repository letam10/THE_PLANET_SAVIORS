# THE_PLANET_SAVIORS — Codex Workflow

## 0. Core Notes
- **Tools:** Always use MCP tools to edit scenes, prefabs, and components instead of manually editing text files.
- **Safety:** Always check for compilation errors before committing.

## 0.1 Browser Input Workflow
Apply this strictly to every browser input field:
1. **Click/focus** the input field.
2. **Wait 2–3 seconds** for the browser to stabilize.
3. **Type the content**.
4. **Wait 1 second**.
5. **Verify the rendered result** by taking a screenshot (Snapshot).
6. **Submit**.

## 1. Mandatory Workflow & Planning
- For every complex task, **you must create a plan in a Markdown table** (using the format in Section 5).
- After presenting the plan table, **stop and wait for explicit user approval** before proceeding.
- **Branch Rule:** Always work on the current branch. **Never work directly on `main`**. Prefer branch names using the format: `codex/<task-name>`.
- **Break work into checkpoints:**
  1. Review the current git diff.
  2. Stage the relevant files. **IMPORTANT: Always stage the corresponding `.meta` files together with changed assets/scripts.**
  3. Create one commit for that checkpoint.
  4. Move to the next checkpoint.

## 2. Unity & Architecture Execution Rules (TPS Project Rules)
- **Folder structure:** Source code must be stored strictly under `Assets/_TPS/Scripts/...`, and content/data under `Assets/_TPS/Data/...`. **Never modify** `Library`, `Temp`, `Logs`, or `PackageCache`.
- **Namespaces:** Must follow the `TPS.[Module]` convention (e.g. `TPS.Runtime.Combat`).
- **Data-driven design:**
  - **ScriptableObjects (SOs)** are templates only (read-only at runtime). **Never** write runtime state into SOs.
  - **Runtime state** must be stored in POCO classes (e.g. `SaveData`) and managed through `GameStateManager`.
- **Events:** Use `GameEventBus` **only** for macro-level events. Use direct references (`[SerializeField]`) or C# actions for internal logic.

## 3. Commit Policy
- **Avoid large commits:** Do not accumulate everything into one huge commit at the end of the day. Prefer small, clear commits.
- **Standard prefixes:** Use `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`, `ui:`.
- If the diff is too large, split it before committing.
- **DATA PROTECTION:** **Never** commit scenes (`.unity`) or prefabs (`.prefab`) if the task only requires C# code changes, in order to avoid serialization conflicts.
- **Do not create PRs or merge into `main`** unless explicitly asked.

## 4. Validation Before Commit
- If code has changed, run the smallest possible validation steps before each checkpoint. Prefer narrow validation over a full project rebuild.
- **Unity-specific checks:**
  - Ensure there are no new compiler errors or warnings in the Unity Console.
  - Ensure there are no broken references (`Missing (Mono Script)` or `Null Reference`) in the scenes/prefabs that were touched.
- If validation fails: **you must fix the issue first** or stop and clearly report it to the user. Do not commit broken code.

## 5. Planning Table Format
| Section | Content |
|---|---|
| Goal | Objective of the task |
| Files & Assets to change | Include affected `.cs`, `.prefab`, `.asset`, and `.meta` files |
| Risks | Risks such as Save/Load issues, prefab overrides, or event conflicts |
| Validation | Validation steps (e.g. "Compile without errors, check Inspector references") |
| Commit message | Planned commit message |