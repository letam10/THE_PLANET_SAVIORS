using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using UnityEngine;

namespace TPS.Runtime.Core
{
    [CreateAssetMenu(fileName = "CAT_Phase1Content", menuName = "TPS/RPG/Phase 1 Content Catalog")]
    public sealed class Phase1ContentCatalog : ScriptableObject
    {
        [Header("Defaults")]
        [SerializeField] private ProgressionCurveDefinition _progressionCurve;
        [Min(0)] [SerializeField] private int _startingCurrency = 120;

        [Header("Boot Content")]
        [SerializeField] private List<CharacterDefinition> _startingPartyMembers = new List<CharacterDefinition>();

        [Header("Definitions")]
        [SerializeField] private List<CharacterDefinition> _characters = new List<CharacterDefinition>();
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();
        [SerializeField] private List<ItemDefinition> _items = new List<ItemDefinition>();
        [SerializeField] private List<EquipmentDefinition> _equipment = new List<EquipmentDefinition>();
        [SerializeField] private List<SkillDefinition> _skills = new List<SkillDefinition>();
        [SerializeField] private List<StatusEffectDefinition> _statuses = new List<StatusEffectDefinition>();
        [SerializeField] private List<RewardTableDefinition> _rewardTables = new List<RewardTableDefinition>();
        [SerializeField] private List<EncounterDefinition> _encounters = new List<EncounterDefinition>();
        [SerializeField] private List<EncounterTableDefinition> _encounterTables = new List<EncounterTableDefinition>();
        [SerializeField] private List<ZoneDefinition> _zones = new List<ZoneDefinition>();
        [SerializeField] private List<ShopDefinition> _shops = new List<ShopDefinition>();
        [SerializeField] private List<DialogueDefinition> _dialogues = new List<DialogueDefinition>();
        [SerializeField] private List<QuestDefinition> _quests = new List<QuestDefinition>();

        public ProgressionCurveDefinition ProgressionCurve => _progressionCurve;
        public int StartingCurrency => _startingCurrency;
        public IReadOnlyList<CharacterDefinition> StartingPartyMembers => _startingPartyMembers;
        public IReadOnlyList<CharacterDefinition> Characters => _characters;
        public IReadOnlyList<EnemyDefinition> Enemies => _enemies;
        public IReadOnlyList<ItemDefinition> Items => _items;
        public IReadOnlyList<EquipmentDefinition> Equipment => _equipment;
        public IReadOnlyList<SkillDefinition> Skills => _skills;
        public IReadOnlyList<StatusEffectDefinition> Statuses => _statuses;
        public IReadOnlyList<RewardTableDefinition> RewardTables => _rewardTables;
        public IReadOnlyList<EncounterDefinition> Encounters => _encounters;
        public IReadOnlyList<EncounterTableDefinition> EncounterTables => _encounterTables;
        public IReadOnlyList<ZoneDefinition> Zones => _zones;
        public IReadOnlyList<ShopDefinition> Shops => _shops;
        public IReadOnlyList<DialogueDefinition> Dialogues => _dialogues;
        public IReadOnlyList<QuestDefinition> Quests => _quests;

        public CharacterDefinition GetCharacter(string characterId) => FindById(_characters, characterId, definition => definition.CharacterId);
        public EnemyDefinition GetEnemy(string enemyId) => FindById(_enemies, enemyId, definition => definition.EnemyId);
        public ItemDefinition GetItem(string itemId) => FindById(_items, itemId, definition => definition.ItemId);
        public EquipmentDefinition GetEquipment(string equipmentId) => FindById(_equipment, equipmentId, definition => definition.EquipmentId);
        public SkillDefinition GetSkill(string skillId) => FindById(_skills, skillId, definition => definition.SkillId);
        public RewardTableDefinition GetReward(string rewardId) => FindById(_rewardTables, rewardId, definition => definition.RewardId);
        public EncounterDefinition GetEncounter(string encounterId) => FindById(_encounters, encounterId, definition => definition.EncounterId);
        public ZoneDefinition GetZone(string zoneId) => FindById(_zones, zoneId, definition => definition.ZoneId);
        public ShopDefinition GetShop(string shopId) => FindById(_shops, shopId, definition => definition.ShopId);
        public DialogueDefinition GetDialogue(string dialogueId) => FindById(_dialogues, dialogueId, definition => definition.DialogueId);
        public QuestDefinition GetQuest(string questId) => FindById(_quests, questId, definition => definition.QuestId);

        private static T FindById<T>(IReadOnlyList<T> definitions, string id, System.Func<T, string> selector) where T : Object
        {
            if (definitions == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                T definition = definitions[i];
                if (definition != null && selector(definition) == id)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
