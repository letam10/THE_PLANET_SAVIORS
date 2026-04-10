# Final Verification Pack

`Final Verification Pack` của branch này luôn có đúng 4 phần:
- `Compile & Audit`
- `System Tests`
- `Manual Final Core Smoke`
- `Reopen Proof Smoke`

## 1. Compile & Audit
- Recompile scripts trong Unity.
- Chạy `Tools/TPS/Functional Lock/Run Final Validation Pack` hoặc tối thiểu:
  - `Tools/TPS/Content/Run Content Validation`
  - `Tools/TPS/Phase 1/Run Project Audit`
- Expected result:
  - compile clean
  - content validation pass
  - audit pass
  - build path vẫn là `Bootstrap -> Core -> ZN_Town_AsterHarbor -> BTL_Standard`
  - no broken refs trên touched scenes/prefabs/assets

## 2. System Tests
- Chạy EditMode tests hiện có cho Phase 1 và Phase 2/3.
- Bắt buộc bao phủ:
  - stat snapshot / computed stats
  - equip/unequip clamp
  - reward -> inventory/currency/exp/progression
  - save schema v2 / invalid save reject
  - validator contract checks
- PlayMode tests chỉ chạy nếu có case ngắn, ổn định, maintenance thấp.

## 3. Manual Final Core Smoke
- Chạy `Tools/TPS/Functional Lock/Prepare Final Core Smoke`.
- Press Play từ `Bootstrap`.
- Xác nhận trên HUD / smoke panel:
  1. vào `ZN_Town_AsterHarbor`
  2. main NPC đúng lịch `07:00-12:00`
  3. đổi `Rain` thì NPC đổi chỗ/visibility đúng
  4. nhận quest và dialogue đổi đúng
  5. vào encounter
  6. clear sub-boss
  7. battle consequence trả về world
  8. encounter table / dialogue / NPC / shop / reward / progression đổi đúng
  9. sleep qua ngày
  10. refresh đúng
  11. save
  12. load
  13. smoke panel báo `Load restored smoke-critical state correctly.`
- Chạy thêm proof content nhỏ:
  - nói chuyện `Quartermaster Ivo`
  - nhận `Secure Dock Supplies`
  - clear `ENC_DockRainMites_Anchor`
  - turn in quest
  - xác nhận `dock_supplies_secured`
  - save/load lại và fact vẫn đúng

## 4. Reopen Proof Smoke
- Đóng Unity/project.
- Mở lại project.
- Chạy lại:
  - `Tools/TPS/Content/Run Content Validation`
  - `Tools/TPS/Phase 1/Run Project Audit`
  - `Tools/TPS/Functional Lock/Prepare Final Core Smoke`
- Press Play từ `Bootstrap`.
- Chạy smoke ngắn:
  - vào town
  - nhìn smoke panel / HUD
  - xác nhận world-state mirrors, shop availability, encounter table, quest state, dock proof fact vẫn đúng
- Expected result:
  - no broken refs sau reopen
  - installer/audit vẫn ổn
  - state mirrors/readout vẫn khớp runtime thật

## Failure Policy
- Nếu `Compile & Audit` fail: không được tiếp tục coi branch là `functional lock`.
- Nếu `System Tests` fail: fix hoặc ghi limitation thật rõ trước khi chốt.
- Nếu `Manual Final Core Smoke` fail: treat như blocker của Phase 4/5.
- Nếu `Reopen Proof Smoke` fail: treat như blocker của Phase 5/6 vì project chưa art-ready đủ an toàn.
