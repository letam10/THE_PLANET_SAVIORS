# Feature Lock Matrix

`Must Fix Before Lock` chỉ áp dụng cho hạng mục thỏa ít nhất một điều kiện:
- làm content production sau này không thể tiếp tục an toàn
- làm save/load/world-state/contracts không đáng tin
- làm validator/audit/tooling không đủ để người làm content kiểm tra
- gây mơ hồ lớn cho gameplay contract hoặc art integration

| System | Current Status | Why | Blocking Production? | Action | Phase | Final Decision |
|---|---|---|---|---|---|---|
| Quest | Production-ready | Quest raw state, objective completion, reward handoff, save/load đã có owner rõ | No | Verify bằng final core smoke và reopen proof | 4 | Locked |
| Dialogue | Production-ready | Branching theo condition, persistent flags, one-shot, quest reaction đã đủ cho bounded content | No | Keep baseline, validate side quest + save/load | 4 | Locked |
| Party | Production-ready | Recruit, active slots, current HP/MP, equipment state, sleep restore đã ổn | No | Keep baseline, verify restore/save path | 4 | Locked |
| Encounter/Battle | Production-ready prototype | Encounter anchor -> battle -> reward/world consequence đã khép kín cho slice | No | Validate world consequence và return flow bằng smoke | 4 | Locked |
| Reward | Production-ready | Battle/quest reward áp vào inventory/currency/EXP và HUD/smoke readout được | No | Keep and verify summary/readability | 4 | Locked |
| Inventory | Production-ready | Item/equipment ownership, consumable use, sell guard theo equipped state đã có | No | Keep and verify with shop/save/load | 4 | Locked |
| Equipment | Production-ready | Equip/unequip đi qua stat snapshot, resource clamp đã khóa | No | Keep and verify clamp/readout | 4 | Locked |
| Progression | Production-ready prototype | EXP/level/unlock/passive mỏng nhưng đủ cho production content hiện tại | No | Freeze breadth, only bugfix if needed | 4 | Locked |
| Economy/Shop | Production-ready | Buy/sell/restock/currency/save-load đã đủ cho current content scale | No | Keep and verify daily refresh + save/load | 4 | Locked |
| Sleep/Day Advance | Production-ready | Sleep restores party, advances clock, refreshes shops/world systems | No | Verify via final smoke and reopen smoke | 4 | Locked |
| Save/Load | Must verify, not redesign | Owner-state restore v2 rõ nhưng phải luôn qua final verification pack | Yes, if broken | Harden only if final verification exposes mismatch | 4-5 | Locked after verification |
| NPC Schedule | Production-ready | Time/weather-driven presence và derived mirror đã đủ cho art/content work | No | Keep and audit target markers after scene changes | 4 | Locked |
| World Reaction | Production-ready prototype | Resolver-driven dialogue/NPC/shop/encounter table/world fact reactions đã có | No | Verify main quest + dock proof consequence | 4 | Locked |
| HUD/Debug Readout | Must improve for handoff | Cần đủ rõ cho QA/content author thay vì chỉ hỗ trợ smoke cũ | Yes, if too opaque | Harden readout and final smoke visibility | 5 | Locked |
| Seeder/Installer | Production-ready | Installer/seeder đang là wiring path chính và đã idempotent ở mức Phase 3 | No | Keep and rerun in final validation pack | 5 | Locked |
| Validation/Audit | Must improve for final lock | Cần bắt được contract/reference/content mistakes rõ hơn trước art integration | Yes, if too weak | Extend validator + audit and freeze workflow | 5 | Locked |
| Content Pipeline | Production-ready for bounded additions | Definition + installer + validator path đã đủ để author thêm content nhỏ an toàn | No | Keep bounded, document exact workflow | 5 | Locked |
| Art Integration Hooks | Must document clearly | Artist/designer cần biết cái gì thay được và cái gì không được phá | Yes, if unclear | Ship short art integration contract | 6 | Locked |
