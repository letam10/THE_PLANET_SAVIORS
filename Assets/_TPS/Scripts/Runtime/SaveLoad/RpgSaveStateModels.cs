using System;
using System.Collections.Generic;
using TPS.Runtime.Quest;

namespace TPS.Runtime.SaveLoad
{
    [Serializable]
    public sealed class StringListEntry
    {
        public string Value;
    }

    [Serializable]
    public sealed class IntMapEntry
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class BoolMapEntry
    {
        public string Key;
        public bool Value;
    }

    [Serializable]
    public sealed class StringMapEntry
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public sealed class DialogueChoiceStateEntry
    {
        public string DialogueId;
        public string ChoiceId;
    }

    [Serializable]
    public sealed class DialogueAffectionStateEntry
    {
        public string EntityId;
        public int Value;
    }

    [Serializable]
    public sealed class QuestProgressStateEntry
    {
        public string QuestId;
        public QuestStatus Status;
        public List<string> CompletedObjectiveIds = new List<string>();
    }

    [Serializable]
    public sealed class DialogueStateData
    {
        public List<StringListEntry> OpenFlags = new List<StringListEntry>();
        public List<DialogueChoiceStateEntry> ChosenChoices = new List<DialogueChoiceStateEntry>();
        public List<StringListEntry> ConsumedOneShots = new List<StringListEntry>();
        public List<DialogueAffectionStateEntry> RelationshipValues = new List<DialogueAffectionStateEntry>();
    }

    [Serializable]
    public sealed class QuestStateData
    {
        public List<QuestProgressStateEntry> Quests = new List<QuestProgressStateEntry>();
    }

    [Serializable]
    public sealed class PartyMemberStateData
    {
        public string CharacterId;
        public bool Recruited;
        public int ActiveSlot = -1;
        public string EquippedWeaponId;
        public int CurrentHP;
        public int CurrentMP;
        public bool IsKnockedOut;
    }

    [Serializable]
    public sealed class PartyStateData
    {
        public List<PartyMemberStateData> Members = new List<PartyMemberStateData>();
    }

    [Serializable]
    public sealed class InventoryStackStateEntry
    {
        public string DefinitionId;
        public int Count;
    }

    [Serializable]
    public sealed class InventoryStateData
    {
        public List<InventoryStackStateEntry> ItemStacks = new List<InventoryStackStateEntry>();
        public List<InventoryStackStateEntry> EquipmentStacks = new List<InventoryStackStateEntry>();
    }

    [Serializable]
    public sealed class ProgressionMemberStateData
    {
        public string CharacterId;
        public int Level = 1;
        public int CurrentExp = 0;
        public List<string> UnlockedSkillIds = new List<string>();
    }

    [Serializable]
    public sealed class ProgressionStateData
    {
        public List<ProgressionMemberStateData> Members = new List<ProgressionMemberStateData>();
    }

    [Serializable]
    public sealed class EncounterStateData
    {
        public List<StringListEntry> ClearedEncounterIds = new List<StringListEntry>();
    }

    [Serializable]
    public sealed class ZoneStateData
    {
        public List<BoolMapEntry> BoolFacts = new List<BoolMapEntry>();
        public List<IntMapEntry> IntFacts = new List<IntMapEntry>();
        public List<StringMapEntry> StringFacts = new List<StringMapEntry>();
    }

    [Serializable]
    public sealed class EconomyStateData
    {
        public int Currency = 0;
        public List<BoolMapEntry> ShopUnlocks = new List<BoolMapEntry>();
        public List<IntMapEntry> ShopStock = new List<IntMapEntry>();
        public List<IntMapEntry> LastRestockDays = new List<IntMapEntry>();
    }
}
