# THE_PLANET_SAVIORS — Codex workflow

## Mandatory workflow
- For any non-trivial task, start by producing a plan as a markdown table.
- The plan must include: Goal, Files to change, Risks, Validation steps, Commit message.
- After producing the plan, stop and wait for explicit user approval.
- Do not edit files until the user replies exactly: APPROVE PLAN.

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