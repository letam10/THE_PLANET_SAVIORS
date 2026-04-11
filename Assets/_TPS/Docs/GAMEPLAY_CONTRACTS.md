# Gameplay Contracts

## Ownership Summary
- `WorldClock` là owner duy nhất của time.
- `WeatherSystem` là owner duy nhất của weather.
- Raw gameplay state chỉ sống trong owner services: quest, dialogue persistent state, party, inventory, progression, encounter clears, zone facts, economy/shop facts.
- `StateResolver` chỉ recompute derived state.
- `GameStateManager` chỉ mirror/debug/bridge, không phải source of truth.

## Stats / Equipment / Progression
- `CharacterStatSnapshot` và `ComputedStats` là stat source cho battle, HUD, reward/progression feedback, consumable clamp, equip/unequip.
- Final stat pipeline:
  - base archetype
  - level growth
  - equipped weapon stat bonus
  - passive/unlock modifiers
  - resistance modifiers
- Resource clamp là bắt buộc khi:
  - load save
  - equip
  - unequip
  - level up
  - passive/unlock refresh
- `CurrentHP` và `CurrentMP` không được âm, không được vượt max mới.

## Character / Enemy / Encounter
- `CharacterDefinition` phải có archetype hợp lệ; starting weapon nếu có phải là weapon thật.
- `EnemyDefinition` là source of truth cho enemy stats, resistance, skill list.
- `EncounterAnchor` chỉ được dùng theo một trong hai mode:
  - direct encounter
  - zone encounter table
- `EncounterService` giữ raw encounter clears.
- Encounter table hiện tại là derived state, không được persist raw.

## Quest / Dialogue / World Reaction
- `QuestService` giữ raw quest status + completed objective ids.
- `DialogueStateService` chỉ giữ:
  - chosen choices
  - opened flags
  - consumed one-shots
  - optional lightweight relationship state
- Dialogue variant hiện tại là derived state.
- Quest/dialogue/world reactions phải đi qua conditions + resolver, không hardcode theo scene state tạm thời.
- `NPCSchedule` là derived world behavior: time/weather/conditions -> marker + visibility.

## Reward / Economy / Shop
- `RewardService` áp reward vào:
  - inventory
  - currency
  - EXP/progression
- `EconomyService` giữ currency, shop unlock state, stock, restock markers.
- Selling equipment không được làm inventory thấp hơn số bản đang equip.
- Shop refresh tiếp tục theo day advance / sleep; không có dynamic pricing trong branch này.

## Save / Load / Resolver
- Save schema hiện tại là `SaveVersion = 2`.
- Save chỉ serialize owner state và world owner state; không serialize derived state.
- Load order bắt buộc:
  - load scene
  - restore time
  - restore weather
  - restore raw owner services
  - publish loaded event
  - resolver recompute derived state
  - teleport player
- Save version cũ hoặc invalid phải bị từ chối rõ ràng; không partial-load im lặng.

## Content Authoring Defaults
- Ưu tiên author qua data + installer/seeder/helper, không hand-wire scene khi cùng kết quả có thể seed/install.
- Mọi content mới phải qua:
  - content validation
  - project audit
  - manual smoke hoặc short proof readout
- `Phase1ContentCatalog` hiện là shared registry tạm thời cho content hiện tại; không phải gameplay owner.
