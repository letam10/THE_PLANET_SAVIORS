# Environment Scaffolding Guide

## Summary
- `AsterHarbor` hiện có một generated environment layer replace-safe dưới `WorldRoot/ENV_AsterHarbor_Generated`.
- Layer này là placeholder-first, deterministic, và có thể rebuild bằng tooling.
- Mục tiêu là tiết kiệm thời gian blockout/layout/ambient dressing, không phải final art.

## Regenerate Flow
- Rebuild toàn bộ scaffolding:
  - `Tools/TPS/Environment/Rebuild Replace-Safe Environment`
- Chỉ rebuild blockout nền:
  - `Tools/TPS/Environment/Generate AsterHarbor Blockout`
- Chỉ rebuild ambient layer:
  - `Tools/TPS/Environment/Rebuild Ambient Layer`
- Validate layout replace-safe:
  - `Tools/TPS/Environment/Validate Replace-Safe Layout`

## Replace-Safe Rules
- Safe to replace:
  - placeholder visuals trong các slot generated
  - building shells
  - roofs
  - fences
  - crates
  - lamps
  - landmark props
  - trees / clutter / ambient placeholders
- Must keep:
  - slot root có `EnvironmentGeneratedMarker`
  - gameplay hooks ngoài generated layer
  - quest/dialogue/merchant/inn/encounter objects
- Khi thay art:
  - thêm child visual mới vào slot root
  - không sửa/xóa hook gameplay
  - không dùng child `GEN_*` cho art thật

## Layer Layout
- `ENV_Blockout`: pads, paths, docks, frontage surfaces
- `ENV_Buildings`: placeholder house/shed/stall shells
- `ENV_Props`: fence lines, cargo, signs, lamps, landmarks
- `ENV_Vegetation`: deterministic trees và clutter nhẹ
- `ENV_Ambient`: filler citizens và ambient creature placeholders
- `ENV_Debug`: landmark/debug readability helpers

## Validation After Replace
1. `Tools/TPS/Content/Run Content Validation`
2. `Tools/TPS/Environment/Validate Replace-Safe Layout`
3. `Tools/TPS/Phase 1/Run Project Audit`
4. Nếu đụng smoke path thì `Tools/TPS/Functional Lock/Prepare Final Core Smoke`

## Known Limits
- Generated ambient layer không phải world simulation mới; nó chỉ là staging placeholder.
- Regenerate sẽ rebuild managed `GEN_*` placeholders.
- Nếu muốn preserve art thật, đặt art dưới slot root bằng child riêng không mang marker generated.
