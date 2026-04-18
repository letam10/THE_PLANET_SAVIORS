# THE_PLANET_SAVIORS — Codex workflow

## 0. LƯU Ý
- Luôn giao tiếp bằng tiếng Việt.
- Luôn sử dụng MCP tools để chỉnh sửa scene, prefab, component trong Unity thay vì sửa file văn bản thuần túy.
- Luôn kiểm tra lỗi biên dịch (Compiler errors) trước khi commit.

## 0.1 Browser Input Workflow (Must Follow)
Áp dụng cho mọi ô nhập dữ liệu trên trình duyệt:
1. **Click/Focus** vào ô nhập liệu.
2. **Đợi 2-3 giây** để trình duyệt ổn định.
3. **Nhập nội dung**.
4. **Đợi 1 giây**.
5. **Kiểm tra hiển thị** (Snapshot).
6. **Nhấn gửi**.

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