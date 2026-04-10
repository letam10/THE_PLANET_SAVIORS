# Functional Lock Decision

## Decision
- Mục tiêu của branch này là đạt `functional lock` cho gameplay backbone và content-production pipeline hiện tại.
- Sau khi `Final Verification Pack` pass ở mức chấp nhận được, dự án được xem là đủ sẵn sàng để chuyển trọng tâm sang:
  - art production
  - map/environment art
  - character art
  - props/trang trí
  - content visual production

## Locked Systems
- quest
- dialogue
- party
- encounter/battle backbone
- reward
- inventory
- equipment
- progression loop mỏng nhưng thật
- economy/shop loop functional
- sleep/day advance
- save/load v2
- NPC schedule / world reaction
- installer / seeder / validation / audit / smoke tooling

## Intentionally Deferred
- full gameplay breadth ngoài `AsterHarbor`
- zone lớn mới
- cinematic / presentation polish
- final shipping UI polish
- deep automation-bridge work
- major folder/domain reorg
- feature expansion beyond current bounded proof content

## Known Limitations Accepted
- full 13-step automation smoke vẫn không phải nguồn sự thật; manual smoke vẫn là proof path trung thực cho end-to-end flow
- `Phase1ContentCatalog` giữ legacy name tạm thời nhưng đang đóng vai trò shared registry
- UI hiện tại là developer/content-author facing, không phải final player-facing production UI

## Rule After Phase 6
- Sau khi Phase 6 kết thúc, mọi feature gameplay mới mặc định bị xem là `deferred`, trừ khi có quyết định riêng bằng văn bản.
- Code mới sau mốc này chỉ nên là:
  - bugfix nhỏ
  - integration support cho art/content/map production
  - fix blocker thật sự do final verification hoặc reopen proof phát hiện
