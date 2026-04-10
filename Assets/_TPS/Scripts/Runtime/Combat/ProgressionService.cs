using System.Collections.Generic;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public sealed class ProgressionService : MonoBehaviour
    {
        private sealed class MemberProgressState
        {
            public CharacterDefinition Definition;
            public int Level = 1;
            public int CurrentExp = 0;
            public readonly HashSet<string> UnlockedSkillIds = new HashSet<string>();
        }

        public static ProgressionService Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private readonly Dictionary<string, MemberProgressState> _memberStates = new Dictionary<string, MemberProgressState>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCatalogDefaults();
        }

        public void EnsureMember(CharacterDefinition characterDefinition)
        {
            if (characterDefinition != null)
            {
                GetOrCreateState(characterDefinition);
            }
        }

        public int GetLevel(string characterId)
        {
            return _memberStates.TryGetValue(characterId, out MemberProgressState state) ? state.Level : 1;
        }

        public int GetCurrentExp(string characterId)
        {
            return _memberStates.TryGetValue(characterId, out MemberProgressState state) ? state.CurrentExp : 0;
        }

        public CharacterStatSnapshot GetCharacterSnapshot(string characterId)
        {
            if (!_memberStates.TryGetValue(characterId, out MemberProgressState state) || state.Definition == null)
            {
                return null;
            }

            EquipmentDefinition equippedWeapon = PartyService.Instance != null
                ? PartyService.Instance.GetEquippedWeapon(characterId)
                : state.Definition.StartingWeapon;

            return BuildCharacterSnapshot(state.Definition, equippedWeapon);
        }

        public void AddExpToParty(int amount, IReadOnlyList<string> characterIds)
        {
            if (amount <= 0 || characterIds == null)
            {
                return;
            }

            for (int i = 0; i < characterIds.Count; i++)
            {
                AddExp(characterIds[i], amount);
            }
        }

        public void AddExp(string characterId, int amount)
        {
            if (amount <= 0 || !_memberStates.TryGetValue(characterId, out MemberProgressState state))
            {
                return;
            }

            ProgressionCurveDefinition curve = _contentCatalog != null ? _contentCatalog.ProgressionCurve : null;
            if (curve == null)
            {
                return;
            }

            state.CurrentExp += amount;
            bool leveledUp = false;
            while (state.CurrentExp >= curve.GetRequiredExpForLevel(state.Level))
            {
                state.CurrentExp -= curve.GetRequiredExpForLevel(state.Level);
                state.Level++;
                leveledUp = true;
                GameEventBus.PublishLevelUp(characterId, state.Level);
            }

            RefreshUnlockedSkills(state);
            if (leveledUp && PartyService.Instance != null)
            {
                PartyService.Instance.ClampResourcesForMember(characterId);
            }

            GameEventBus.PublishProgressionChanged(characterId);
        }

        public IReadOnlyCollection<string> GetUnlockedSkillIds(string characterId)
        {
            return _memberStates.TryGetValue(characterId, out MemberProgressState state)
                ? state.UnlockedSkillIds
                : (IReadOnlyCollection<string>)System.Array.Empty<string>();
        }

        public void RefreshDerivedProgression(string characterId)
        {
            if (!_memberStates.TryGetValue(characterId, out MemberProgressState state))
            {
                return;
            }

            RefreshUnlockedSkills(state);
            if (PartyService.Instance != null)
            {
                PartyService.Instance.ClampResourcesForMember(characterId);
            }

            GameEventBus.PublishProgressionChanged(characterId);
        }

        public CharacterStatSnapshot BuildCharacterSnapshot(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            if (characterDefinition == null)
            {
                return null;
            }

            MemberProgressState state = GetOrCreateState(characterDefinition);
            var snapshot = new CharacterStatSnapshot
            {
                CharacterId = characterDefinition.CharacterId,
                DisplayName = characterDefinition.DisplayName,
                Level = state.Level,
                EquippedWeapon = equippedWeapon,
                Stats = ComputedStats.FromBase(characterDefinition.Archetype != null ? characterDefinition.Archetype.BaseStats : null),
                ResistanceProfile = characterDefinition.Archetype != null ? characterDefinition.Archetype.BaseResistance.Clone() : new ResistanceProfile()
            };

            ApplyGrowth(snapshot.Stats, characterDefinition, state.Level);
            if (equippedWeapon != null)
            {
                snapshot.Stats.ApplyStatBlock(equippedWeapon.StatBonus);
            }

            if (characterDefinition.ResistanceModifier != null)
            {
                snapshot.ResistanceProfile.Add(characterDefinition.ResistanceModifier);
            }

            if (equippedWeapon != null)
            {
                snapshot.ResistanceProfile.Add(equippedWeapon.ResistanceModifier);
            }

            AddSkills(snapshot.Skills, characterDefinition.StartingSkills);

            IReadOnlyList<SkillUnlockDefinition> unlocks = characterDefinition.Archetype != null
                ? characterDefinition.Archetype.SkillUnlocks
                : System.Array.Empty<SkillUnlockDefinition>();
            for (int i = 0; i < unlocks.Count; i++)
            {
                SkillUnlockDefinition unlock = unlocks[i];
                if (!IsUnlockActive(state, unlock, equippedWeapon))
                {
                    continue;
                }

                string unlockId = GetUnlockId(unlock, i, characterDefinition.CharacterId);
                snapshot.ActiveUnlockIds.Add(unlockId);
                snapshot.Stats.ApplyModifier(unlock.PassiveStatModifier);
                if (unlock.PassiveResistanceModifier != null)
                {
                    snapshot.ResistanceProfile.Add(unlock.PassiveResistanceModifier);
                }

                if (unlock.Skill != null)
                {
                    AddSkill(snapshot.Skills, unlock.Skill);
                }
            }

            if (equippedWeapon != null)
            {
                AddSkills(snapshot.Skills, equippedWeapon.GrantedSkills);
            }

            snapshot.Stats.Clamp();
            return snapshot;
        }

        public StatBlock BuildFinalStats(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            CharacterStatSnapshot snapshot = BuildCharacterSnapshot(characterDefinition, equippedWeapon);
            return snapshot != null ? snapshot.Stats.ToStatBlock() : new StatBlock();
        }

        public ResistanceProfile BuildResistanceProfile(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            CharacterStatSnapshot snapshot = BuildCharacterSnapshot(characterDefinition, equippedWeapon);
            return snapshot != null ? snapshot.ResistanceProfile : new ResistanceProfile();
        }

        public List<SkillDefinition> BuildSkillList(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            CharacterStatSnapshot snapshot = BuildCharacterSnapshot(characterDefinition, equippedWeapon);
            return snapshot != null ? new List<SkillDefinition>(snapshot.Skills) : new List<SkillDefinition>();
        }

        public ProgressionStateData CaptureState()
        {
            var data = new ProgressionStateData();
            foreach (var pair in _memberStates)
            {
                MemberProgressState state = pair.Value;
                if (state == null || state.Definition == null)
                {
                    continue;
                }

                var entry = new ProgressionMemberStateData
                {
                    CharacterId = state.Definition.CharacterId,
                    Level = state.Level,
                    CurrentExp = state.CurrentExp
                };

                foreach (string skillId in state.UnlockedSkillIds)
                {
                    entry.UnlockedSkillIds.Add(skillId);
                }

                data.Members.Add(entry);
            }

            return data;
        }

        public void RestoreState(ProgressionStateData data)
        {
            _memberStates.Clear();
            EnsureCatalogDefaults();

            if (data == null || _contentCatalog == null)
            {
                return;
            }

            for (int i = 0; i < data.Members.Count; i++)
            {
                ProgressionMemberStateData entry = data.Members[i];
                CharacterDefinition definition = _contentCatalog.GetCharacter(entry.CharacterId);
                if (definition == null)
                {
                    continue;
                }

                MemberProgressState state = GetOrCreateState(definition);
                state.Level = Mathf.Max(1, entry.Level);
                state.CurrentExp = Mathf.Max(0, entry.CurrentExp);
                state.UnlockedSkillIds.Clear();
                for (int skillIndex = 0; skillIndex < entry.UnlockedSkillIds.Count; skillIndex++)
                {
                    string skillId = entry.UnlockedSkillIds[skillIndex];
                    if (!string.IsNullOrWhiteSpace(skillId))
                    {
                        state.UnlockedSkillIds.Add(skillId);
                    }
                }

                RefreshUnlockedSkills(state);
            }
        }

        private void EnsureCatalogDefaults()
        {
            if (_contentCatalog == null)
            {
                return;
            }

            IReadOnlyList<CharacterDefinition> characters = _contentCatalog.Characters;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition definition = characters[i];
                if (definition != null)
                {
                    GetOrCreateState(definition);
                }
            }
        }

        private MemberProgressState GetOrCreateState(CharacterDefinition characterDefinition)
        {
            if (!_memberStates.TryGetValue(characterDefinition.CharacterId, out MemberProgressState state))
            {
                state = new MemberProgressState
                {
                    Definition = characterDefinition,
                    Level = Mathf.Max(1, characterDefinition.StartingLevel)
                };
                _memberStates[characterDefinition.CharacterId] = state;
                RefreshUnlockedSkills(state);
            }

            return state;
        }

        private static void RefreshUnlockedSkills(MemberProgressState state)
        {
            state.UnlockedSkillIds.Clear();
            if (state.Definition == null || state.Definition.Archetype == null)
            {
                return;
            }

            WeaponFamilyType equippedFamily = WeaponFamilyType.None;
            if (PartyService.Instance != null)
            {
                EquipmentDefinition equippedWeapon = PartyService.Instance.GetEquippedWeapon(state.Definition.CharacterId);
                if (equippedWeapon != null)
                {
                    equippedFamily = equippedWeapon.WeaponFamily;
                }
            }

            IReadOnlyList<SkillUnlockDefinition> unlocks = state.Definition.Archetype.SkillUnlocks;
            for (int i = 0; i < unlocks.Count; i++)
            {
                SkillUnlockDefinition unlock = unlocks[i];
                if (unlock == null || unlock.Skill == null)
                {
                    continue;
                }

                if (!IsUnlockActive(state, unlock, equippedFamily))
                {
                    continue;
                }

                state.UnlockedSkillIds.Add(unlock.Skill.SkillId);
            }
        }

        private static bool IsUnlockActive(MemberProgressState state, SkillUnlockDefinition unlock, EquipmentDefinition equippedWeapon)
        {
            WeaponFamilyType equippedFamily = equippedWeapon != null ? equippedWeapon.WeaponFamily : WeaponFamilyType.None;
            return IsUnlockActive(state, unlock, equippedFamily);
        }

        private static bool IsUnlockActive(MemberProgressState state, SkillUnlockDefinition unlock, WeaponFamilyType equippedFamily)
        {
            if (state == null || unlock == null)
            {
                return false;
            }

            if (state.Level < unlock.RequiredLevel)
            {
                return false;
            }

            return unlock.RequiredWeaponFamily == WeaponFamilyType.None || unlock.RequiredWeaponFamily == equippedFamily;
        }

        private static string GetUnlockId(SkillUnlockDefinition unlock, int index, string characterId)
        {
            if (unlock == null)
            {
                return $"{characterId}:unlock:{index}";
            }

            if (!string.IsNullOrWhiteSpace(unlock.UnlockId))
            {
                return unlock.UnlockId;
            }

            if (unlock.Skill != null && !string.IsNullOrWhiteSpace(unlock.Skill.SkillId))
            {
                return unlock.Skill.SkillId;
            }

            return $"{characterId}:unlock:{index}";
        }

        private static void ApplyGrowth(ComputedStats stats, CharacterDefinition characterDefinition, int level)
        {
            if (stats == null || characterDefinition == null || characterDefinition.Archetype == null)
            {
                return;
            }

            StatBlock growthStats = characterDefinition.Archetype.GrowthStats;
            if (growthStats == null)
            {
                return;
            }

            int levelOffset = Mathf.Max(0, level - 1);
            stats.MaxHP += growthStats.MaxHP * levelOffset;
            stats.MaxMP += growthStats.MaxMP * levelOffset;
            stats.Attack += growthStats.Attack * levelOffset;
            stats.Magic += growthStats.Magic * levelOffset;
            stats.Defense += growthStats.Defense * levelOffset;
            stats.Resistance += growthStats.Resistance * levelOffset;
            stats.Speed += growthStats.Speed * levelOffset;
        }

        private static void AddSkills(List<SkillDefinition> target, IReadOnlyList<SkillDefinition> skills)
        {
            if (target == null || skills == null)
            {
                return;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                AddSkill(target, skills[i]);
            }
        }

        private static void AddSkill(List<SkillDefinition> target, SkillDefinition skill)
        {
            if (target == null || skill == null || target.Contains(skill))
            {
                return;
            }

            target.Add(skill);
        }
    }
}
