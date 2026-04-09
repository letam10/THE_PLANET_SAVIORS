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
            while (state.CurrentExp >= curve.GetRequiredExpForLevel(state.Level))
            {
                state.CurrentExp -= curve.GetRequiredExpForLevel(state.Level);
                state.Level++;
                RefreshUnlockedSkills(state);
                GameEventBus.PublishLevelUp(characterId, state.Level);
            }

            GameEventBus.PublishProgressionChanged(characterId);
        }

        public IReadOnlyCollection<string> GetUnlockedSkillIds(string characterId)
        {
            return _memberStates.TryGetValue(characterId, out MemberProgressState state)
                ? state.UnlockedSkillIds
                : (IReadOnlyCollection<string>)System.Array.Empty<string>();
        }

        public StatBlock BuildFinalStats(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            MemberProgressState state = GetOrCreateState(characterDefinition);
            StatBlock finalStats = characterDefinition.Archetype != null ? characterDefinition.Archetype.BaseStats.Clone() : new StatBlock();
            StatBlock growthStats = characterDefinition.Archetype != null ? characterDefinition.Archetype.GrowthStats : null;
            int levelOffset = Mathf.Max(0, state.Level - 1);

            if (growthStats != null)
            {
                finalStats.MaxHP += growthStats.MaxHP * levelOffset;
                finalStats.MaxMP += growthStats.MaxMP * levelOffset;
                finalStats.Attack += growthStats.Attack * levelOffset;
                finalStats.Magic += growthStats.Magic * levelOffset;
                finalStats.Defense += growthStats.Defense * levelOffset;
                finalStats.Resistance += growthStats.Resistance * levelOffset;
                finalStats.Speed += growthStats.Speed * levelOffset;
            }

            if (equippedWeapon != null)
            {
                finalStats.Add(equippedWeapon.StatBonus);
            }

            finalStats.MaxHP = Mathf.Max(1, finalStats.MaxHP);
            finalStats.MaxMP = Mathf.Max(0, finalStats.MaxMP);
            return finalStats;
        }

        public ResistanceProfile BuildResistanceProfile(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            ResistanceProfile profile = characterDefinition.Archetype != null ? characterDefinition.Archetype.BaseResistance.Clone() : new ResistanceProfile();
            if (characterDefinition.ResistanceModifier != null)
            {
                profile.Add(characterDefinition.ResistanceModifier);
            }

            if (equippedWeapon != null)
            {
                profile.Add(equippedWeapon.ResistanceModifier);
            }

            return profile;
        }

        public List<SkillDefinition> BuildSkillList(CharacterDefinition characterDefinition, EquipmentDefinition equippedWeapon)
        {
            var skills = new List<SkillDefinition>();
            if (characterDefinition == null)
            {
                return skills;
            }

            for (int i = 0; i < characterDefinition.StartingSkills.Count; i++)
            {
                SkillDefinition skill = characterDefinition.StartingSkills[i];
                if (skill != null && !skills.Contains(skill))
                {
                    skills.Add(skill);
                }
            }

            MemberProgressState state = GetOrCreateState(characterDefinition);
            if (characterDefinition.Archetype != null)
            {
                IReadOnlyList<SkillUnlockDefinition> unlocks = characterDefinition.Archetype.SkillUnlocks;
                for (int i = 0; i < unlocks.Count; i++)
                {
                    SkillUnlockDefinition unlock = unlocks[i];
                    if (unlock != null && unlock.Skill != null && state.UnlockedSkillIds.Contains(unlock.Skill.SkillId) && !skills.Contains(unlock.Skill))
                    {
                        skills.Add(unlock.Skill);
                    }
                }
            }

            if (equippedWeapon != null)
            {
                IReadOnlyList<SkillDefinition> grantedSkills = equippedWeapon.GrantedSkills;
                for (int i = 0; i < grantedSkills.Count; i++)
                {
                    SkillDefinition skill = grantedSkills[i];
                    if (skill != null && !skills.Contains(skill))
                    {
                        skills.Add(skill);
                    }
                }
            }

            return skills;
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

                if (state.Level < unlock.RequiredLevel)
                {
                    continue;
                }

                if (unlock.RequiredWeaponFamily != WeaponFamilyType.None && unlock.RequiredWeaponFamily != equippedFamily)
                {
                    continue;
                }

                state.UnlockedSkillIds.Add(unlock.Skill.SkillId);
            }
        }
    }
}
