# Art Integration Contract

## Safe To Replace
- Visual placeholder meshes, materials, props, banners, shop facades, tavern facade, dock decoration, NPC model visuals.
- Battle/background visual assets nếu không đổi runtime component hooks.
- Character visual placeholders miễn là giữ nguyên required gameplay components và references.

## Must Keep
- `CoreServices` object và các owner services trong `Core.unity`.
- Runtime hooks như:
  - `DialogueAnchor`
  - `EncounterAnchor`
  - `MerchantAnchor`
  - `InnAnchor`
  - `NPCSchedule`
  - `ConditionalActivator`
  - `BattleWorldBridge`
  - `Phase1SmokeRunner`
  - `Phase1RuntimeHUD`
- Serialized ids và key references trên các hook ở trên không được đổi ngẫu nhiên.
- Marker transforms đang được `NPCSchedule` hoặc encounter/world hooks dùng làm target.

## Scene Rules
- Ưu tiên thay art qua prefab/material/model swap; không tự ý xóa object đang giữ anchor/hook.
- Nếu cần đổi object chứa hook, dùng installer/helper hoặc thay object theo cách preserve serialized references.
- Không hand-wire thêm logic scene rời rạc nếu cùng kết quả có thể đạt bằng data + installer/seeder.
- Build path chuẩn phải giữ nguyên: `Bootstrap -> Core -> ZN_Town_AsterHarbor -> BTL_Standard`.

## Validation After Replace
- Sau mỗi batch thay art/prefab/scene:
  - chạy `Tools/TPS/Content/Run Content Validation`
  - chạy `Tools/TPS/Phase 1/Run Project Audit`
  - nếu vừa sửa world/battle hooks thì chạy `Tools/TPS/Functional Lock/Prepare Final Core Smoke`
  - Press Play từ `Bootstrap` và nhìn HUD/smoke panel
- Trước khi chốt batch lớn:
  - rerun `Tools/TPS/Functional Lock/Run Final Validation Pack`
  - làm `Reopen Proof Smoke` theo [FINAL_VERIFICATION_PACK.md](/D:/CODE%20GAME/THE_PLANET_SAVIORS/Assets/_TPS/Docs/FINAL_VERIFICATION_PACK.md)
