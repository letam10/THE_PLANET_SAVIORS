# Art Integration Contract

## Safe To Replace
- Visual placeholder meshes, materials, props, banners, shop facades, tavern facade, dock decoration, NPC model visuals.
- Battle/background visual assets nếu không đổi runtime component hooks.
- Character visual placeholders miễn là giữ nguyên required gameplay components và references.
- Generated environment visuals dưới `ENV_AsterHarbor_Generated`, đặc biệt trong:
  - `ENV_Blockout`
  - `ENV_Buildings`
  - `ENV_Props`
  - `ENV_Vegetation`
  - `ENV_Ambient`
- Với generated building/prop slots, artist có thể thêm child visual mới vào slot root để thay placeholder mà không cần đổi code.

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
- Root generated containers và slot roots có `EnvironmentGeneratedMarker` nên được giữ lại nếu muốn preserve replace-safe regeneration path.

## Scene Rules
- Ưu tiên thay art qua prefab/material/model swap; không tự ý xóa object đang giữ anchor/hook.
- Nếu cần đổi object chứa hook, dùng installer/helper hoặc thay object theo cách preserve serialized references.
- Không hand-wire thêm logic scene rời rạc nếu cùng kết quả có thể đạt bằng data + installer/seeder.
- Build path chuẩn phải giữ nguyên: `Bootstrap -> Core -> ZN_Town_AsterHarbor -> BTL_Standard`.
- Không đặt art thủ công trực tiếp trong generated placeholder child có tên `GEN_*`; hãy thêm art mới làm child riêng dưới slot root để generator không overwrite.

## Validation After Replace
- Sau mỗi batch thay art/prefab/scene:
  - chạy `Tools/TPS/Content/Run Content Validation`
  - chạy `Tools/TPS/Environment/Validate Replace-Safe Layout` nếu batch có thay buildings/props/vegetation/ambient layer
  - chạy `Tools/TPS/Phase 1/Run Project Audit`
  - nếu vừa sửa world/battle hooks thì chạy `Tools/TPS/Functional Lock/Prepare Final Core Smoke`
  - Press Play từ `Bootstrap` và nhìn HUD/smoke panel
- Trước khi chốt batch lớn:
  - rerun `Tools/TPS/Functional Lock/Run Final Validation Pack`
  - làm `Reopen Proof Smoke` theo [FINAL_VERIFICATION_PACK.md](/D:/CODE%20GAME/THE_PLANET_SAVIORS/Assets/_TPS/Docs/FINAL_VERIFICATION_PACK.md)
