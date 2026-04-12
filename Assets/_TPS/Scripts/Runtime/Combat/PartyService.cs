using System.Collections.Generic;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public sealed class PartyService : MonoBehaviour
    {
        private sealed class PartyMemberRuntimeState
        {
            public CharacterDefinition Definition;
            public bool Recruited;
            public int ActiveSlot = -1;
            public string EquippedWeaponId;
            public int CurrentHP;
            public int CurrentMP;
            public bool IsKnockedOut;
        }

        public static PartyService Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private readonly Dictionary<string, PartyMemberRuntimeState> _members = new Dictionary<string, PartyMemberRuntimeState>();
        private bool _defaultsInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDefaults();
        }

        private void OnEnable()
        {
            GameEventBus.OnProgressionChanged += OnProgressionChanged;
        }

        private void OnDisable()
        {
            GameEventBus.OnProgressionChanged -= OnProgressionChanged;
        }

        public void EnsureDefaults()
        {
            if (_defaultsInitialized)
            {
                return;
            }

            _defaultsInitialized = true;
            if (_contentCatalog == null)
            {
                return;
            }

            IReadOnlyList<CharacterDefinition> startingParty = _contentCatalog.StartingPartyMembers;
            for (int i = 0; i < startingParty.Count; i++)
            {
                RecruitMember(startingParty[i], true, i);
            }
        }

        public void RecruitMember(CharacterDefinition characterDefinition, bool activateIfPossible)
        {
            RecruitMember(characterDefinition, activateIfPossible, -1);
        }

        private void RecruitMember(CharacterDefinition characterDefinition, bool activateIfPossible, int forcedSlot)
        {
            if (characterDefinition == null)
            {
                return;
            }

            PartyMemberRuntimeState state = GetOrCreateState(characterDefinition);
            if (!state.Recruited)
            {
                state.Recruited = true;
                if (ProgressionService.Instance != null)
                {
                    ProgressionService.Instance.EnsureMember(characterDefinition);
                }

                if (InventoryService.Instance != null && characterDefinition.StartingWeapon != null && InventoryService.Instance.GetEquipmentCount(characterDefinition.StartingWeapon.EquipmentId) <= 0)
                {
                    InventoryService.Instance.AddEquipment(characterDefinition.StartingWeapon, 1);
                }

                if (string.IsNullOrWhiteSpace(state.EquippedWeaponId) && characterDefinition.StartingWeapon != null)
                {
                    state.EquippedWeaponId = characterDefinition.StartingWeapon.EquipmentId;
                }
            }

            if (activateIfPossible)
            {
                state.ActiveSlot = forcedSlot >= 0 ? forcedSlot : FindNextOpenActiveSlot();
            }

            RestoreToFull(state);
            GameEventBus.PublishPartyChanged(characterDefinition.CharacterId);
        }

        public bool IsMemberRecruited(string characterId)
        {
            return _members.TryGetValue(characterId, out PartyMemberRuntimeState state) && state.Recruited;
        }

        public List<string> GetActiveMemberIds()
        {
            var result = new List<string>();
            foreach (var pair in _members)
            {
                PartyMemberRuntimeState state = pair.Value;
                if (state != null && state.Recruited && state.ActiveSlot >= 0)
                {
                    result.Add(pair.Key);
                }
            }

            result.Sort((left, right) => _members[left].ActiveSlot.CompareTo(_members[right].ActiveSlot));
            return result;
        }

        public List<string> GetRecruitedMemberIds()
        {
            var result = new List<string>();
            foreach (var pair in _members)
            {
                if (pair.Value != null && pair.Value.Recruited)
                {
                    result.Add(pair.Key);
                }
            }

            result.Sort((left, right) =>
            {
                int leftSlot = _members[left].ActiveSlot;
                int rightSlot = _members[right].ActiveSlot;
                if (leftSlot < 0 && rightSlot >= 0) return 1;
                if (rightSlot < 0 && leftSlot >= 0) return -1;
                if (leftSlot != rightSlot) return leftSlot.CompareTo(rightSlot);
                return string.CompareOrdinal(left, right);
            });
            return result;
        }

        public bool SetMemberActiveSlot(string characterId, int slot)
        {
            if (slot < 0 || slot > 2 || !_members.TryGetValue(characterId, out PartyMemberRuntimeState state) || !state.Recruited)
            {
                return false;
            }

            foreach (var pair in _members)
            {
                if (pair.Key != characterId && pair.Value != null && pair.Value.Recruited && pair.Value.ActiveSlot == slot)
                {
                    pair.Value.ActiveSlot = -1;
                }
            }

            state.ActiveSlot = slot;
            GameEventBus.PublishPartyChanged(characterId);
            return true;
        }

        public bool BenchMember(string characterId)
        {
            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state) || !state.Recruited || state.ActiveSlot < 0)
            {
                return false;
            }

            if (CountActiveMembers() <= 1)
            {
                return false;
            }

            state.ActiveSlot = -1;
            GameEventBus.PublishPartyChanged(characterId);
            return true;
        }

        public CharacterDefinition GetCharacterDefinition(string characterId)
        {
            return _members.TryGetValue(characterId, out PartyMemberRuntimeState state) ? state.Definition : null;
        }

        public EquipmentDefinition GetEquippedWeapon(string characterId)
        {
            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state) || string.IsNullOrWhiteSpace(state.EquippedWeaponId) || _contentCatalog == null)
            {
                return null;
            }

            return _contentCatalog.GetEquipment(state.EquippedWeaponId);
        }

        public CharacterStatSnapshot GetMemberSnapshot(string characterId)
        {
            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state) || state.Definition == null || ProgressionService.Instance == null)
            {
                return null;
            }

            return ProgressionService.Instance.BuildCharacterSnapshot(state.Definition, GetEquippedWeapon(characterId));
        }

        public int GetEquippedWeaponCount(string equipmentId, string excludeCharacterId = null)
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
            {
                return 0;
            }

            int count = 0;
            foreach (var pair in _members)
            {
                if (!string.IsNullOrWhiteSpace(excludeCharacterId) && pair.Key == excludeCharacterId)
                {
                    continue;
                }

                PartyMemberRuntimeState state = pair.Value;
                if (state != null && state.Recruited && state.EquippedWeaponId == equipmentId)
                {
                    count++;
                }
            }

            return count;
        }

        public bool EquipWeapon(string characterId, EquipmentDefinition equipmentDefinition)
        {
            if (equipmentDefinition == null || equipmentDefinition.SlotType != EquipmentSlotType.Weapon || InventoryService.Instance == null)
            {
                return false;
            }

            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state) || !state.Recruited)
            {
                return false;
            }

            int ownedCopies = InventoryService.Instance.GetEquipmentCount(equipmentDefinition.EquipmentId);
            int equippedByOthers = GetEquippedWeaponCount(equipmentDefinition.EquipmentId, characterId);
            if (ownedCopies <= equippedByOthers)
            {
                return false;
            }

            state.EquippedWeaponId = equipmentDefinition.EquipmentId;
            if (ProgressionService.Instance != null)
            {
                ProgressionService.Instance.RefreshDerivedProgression(characterId);
            }
            else
            {
                ClampResourcesToMax(state);
            }

            GameEventBus.PublishPartyChanged(characterId);
            return true;
        }

        public bool UnequipWeapon(string characterId)
        {
            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state) || !state.Recruited || string.IsNullOrWhiteSpace(state.EquippedWeaponId))
            {
                return false;
            }

            state.EquippedWeaponId = string.Empty;
            if (ProgressionService.Instance != null)
            {
                ProgressionService.Instance.RefreshDerivedProgression(characterId);
            }
            else
            {
                ClampResourcesToMax(state);
            }

            GameEventBus.PublishPartyChanged(characterId);
            return true;
        }

        public int GetCurrentHP(string characterId)
        {
            return _members.TryGetValue(characterId, out PartyMemberRuntimeState state) ? state.CurrentHP : 0;
        }

        public int GetCurrentMP(string characterId)
        {
            return _members.TryGetValue(characterId, out PartyMemberRuntimeState state) ? state.CurrentMP : 0;
        }

        public void SetCurrentResources(string characterId, int currentHP, int currentMP, bool isKnockedOut)
        {
            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state))
            {
                return;
            }

            state.CurrentHP = Mathf.Max(0, currentHP);
            state.CurrentMP = Mathf.Max(0, currentMP);
            state.IsKnockedOut = isKnockedOut || state.CurrentHP <= 0;
            ClampResourcesToMax(state);
            GameEventBus.PublishPartyChanged(characterId);
        }

        public bool TryUseConsumable(string characterId, ItemDefinition itemDefinition)
        {
            if (string.IsNullOrWhiteSpace(characterId) || itemDefinition == null || InventoryService.Instance == null || !_members.TryGetValue(characterId, out PartyMemberRuntimeState state))
            {
                return false;
            }

            if (!InventoryService.Instance.RemoveItem(itemDefinition, 1))
            {
                return false;
            }

            CharacterStatSnapshot snapshot = GetMemberSnapshot(characterId);
            if (snapshot == null)
            {
                return false;
            }

            int nextHP = Mathf.Clamp(state.CurrentHP + itemDefinition.RestoreHP, 0, snapshot.Stats.MaxHP);
            int nextMP = Mathf.Clamp(state.CurrentMP + itemDefinition.RestoreMP, 0, snapshot.Stats.MaxMP);
            SetCurrentResources(characterId, nextHP, nextMP, false);
            return true;
        }

        public void RestorePartyAfterSleep()
        {
            foreach (var pair in _members)
            {
                PartyMemberRuntimeState state = pair.Value;
                if (state != null && state.Recruited)
                {
                    RestoreToFull(state);
                }
            }

            GameEventBus.PublishPartyChanged("sleep_restore");
        }

        public void ClampResourcesForMember(string characterId)
        {
            if (!_members.TryGetValue(characterId, out PartyMemberRuntimeState state))
            {
                return;
            }

            ClampResourcesToMax(state);
            GameEventBus.PublishPartyChanged(characterId);
        }

        public PartyStateData CaptureState()
        {
            var data = new PartyStateData();
            foreach (var pair in _members)
            {
                PartyMemberRuntimeState state = pair.Value;
                if (state == null || state.Definition == null || !state.Recruited)
                {
                    continue;
                }

                data.Members.Add(new PartyMemberStateData
                {
                    CharacterId = state.Definition.CharacterId,
                    Recruited = state.Recruited,
                    ActiveSlot = state.ActiveSlot,
                    EquippedWeaponId = state.EquippedWeaponId,
                    CurrentHP = state.CurrentHP,
                    CurrentMP = state.CurrentMP,
                    IsKnockedOut = state.IsKnockedOut
                });
            }

            return data;
        }

        public void RestoreState(PartyStateData data)
        {
            _members.Clear();
            _defaultsInitialized = true;

            if (data == null || _contentCatalog == null)
            {
                return;
            }

            for (int i = 0; i < data.Members.Count; i++)
            {
                PartyMemberStateData entry = data.Members[i];
                CharacterDefinition definition = _contentCatalog.GetCharacter(entry.CharacterId);
                if (definition == null)
                {
                    continue;
                }

                PartyMemberRuntimeState state = GetOrCreateState(definition);
                state.Recruited = entry.Recruited;
                state.ActiveSlot = entry.ActiveSlot;
                state.EquippedWeaponId = entry.EquippedWeaponId;
                state.CurrentHP = entry.CurrentHP;
                state.CurrentMP = entry.CurrentMP;
                state.IsKnockedOut = entry.IsKnockedOut;
                ClampResourcesToMax(state);
            }
        }

        private PartyMemberRuntimeState GetOrCreateState(CharacterDefinition definition)
        {
            if (!_members.TryGetValue(definition.CharacterId, out PartyMemberRuntimeState state))
            {
                state = new PartyMemberRuntimeState
                {
                    Definition = definition
                };
                _members[definition.CharacterId] = state;
                RestoreToFull(state);
            }

            return state;
        }

        private int FindNextOpenActiveSlot()
        {
            for (int slot = 0; slot < 3; slot++)
            {
                bool occupied = false;
                foreach (var pair in _members)
                {
                    PartyMemberRuntimeState state = pair.Value;
                    if (state != null && state.Recruited && state.ActiveSlot == slot)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    return slot;
                }
            }

            return -1;
        }

        private int CountActiveMembers()
        {
            int count = 0;
            foreach (var pair in _members)
            {
                if (pair.Value != null && pair.Value.Recruited && pair.Value.ActiveSlot >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private void RestoreToFull(PartyMemberRuntimeState state)
        {
            CharacterStatSnapshot snapshot = ProgressionService.Instance != null
                ? ProgressionService.Instance.BuildCharacterSnapshot(state.Definition, GetEquippedWeapon(state.Definition.CharacterId) ?? state.Definition.StartingWeapon)
                : null;
            state.CurrentHP = snapshot != null ? snapshot.Stats.MaxHP : 1;
            state.CurrentMP = snapshot != null ? snapshot.Stats.MaxMP : 0;
            state.IsKnockedOut = false;
        }

        private void ClampResourcesToMax(PartyMemberRuntimeState state)
        {
            CharacterStatSnapshot snapshot = ProgressionService.Instance != null
                ? ProgressionService.Instance.BuildCharacterSnapshot(state.Definition, GetEquippedWeapon(state.Definition.CharacterId))
                : null;
            int maxHP = snapshot != null ? snapshot.Stats.MaxHP : 1;
            int maxMP = snapshot != null ? snapshot.Stats.MaxMP : 0;
            state.CurrentHP = Mathf.Clamp(state.CurrentHP, 0, maxHP);
            state.CurrentMP = Mathf.Clamp(state.CurrentMP, 0, maxMP);
            if (state.CurrentHP <= 0)
            {
                state.IsKnockedOut = true;
            }
            else if (state.IsKnockedOut)
            {
                state.IsKnockedOut = false;
            }
        }

        private void OnProgressionChanged(string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId))
            {
                return;
            }

            if (_members.TryGetValue(memberId, out PartyMemberRuntimeState state))
            {
                ClampResourcesToMax(state);
            }
        }
    }
}
