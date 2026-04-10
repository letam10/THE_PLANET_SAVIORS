# THE_PLANET_SAVIORS — Codex workflow

## Mandatory workflow
- For any non-trivial task, start by producing a plan as a markdown table.
- The plan must include: Goal, Files to change, Risks, Validation steps, Commit message.
- After producing the plan, stop and wait for explicit user approval.
- Do not edit files until the user replies exactly: APPROVE PLAN.
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