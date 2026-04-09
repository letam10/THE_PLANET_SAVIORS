using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace TPS.Editor
{
    internal sealed class Phase1Assets
    {
        public Phase1ContentCatalog Catalog;
        public ProgressionCurveDefinition ProgressionCurve;
        public StatusEffectDefinition Poison;
        public StatusEffectDefinition Burn;
        public StatusEffectDefinition Wet;
        public StatusEffectDefinition GuardBreak;
        public SkillDefinition GuardBreakSkill;
        public SkillDefinition FireBurstSkill;
        public SkillDefinition HealingWaveSkill;
        public SkillDefinition ShockShotSkill;
        public SkillDefinition RaiderStrikeSkill;
        public SkillDefinition StormSpitSkill;
        public ItemDefinition Potion;
        public ItemDefinition Ether;
        public EquipmentDefinition BronzeBlade;
        public EquipmentDefinition FocusWand;
        public EquipmentDefinition HunterBow;
        public EquipmentDefinition IronPike;
        public CharacterArchetypeDefinition Vanguard;
        public CharacterArchetypeDefinition Ranger;
        public CharacterArchetypeDefinition Mystic;
        public CharacterDefinition Ari;
        public CharacterDefinition Noa;
        public CharacterDefinition Lina;
        public EnemyDefinition RaiderScout;
        public EnemyDefinition RaiderCaptain;
        public EnemyDefinition RainMite;
        public RewardTableDefinition ScoutReward;
        public RewardTableDefinition CaptainReward;
        public RewardTableDefinition QuestReward;
        public EncounterDefinition HarborScoutEncounter;
        public EncounterDefinition HarborCaptainEncounter;
        public EncounterDefinition PostBossEncounter;
        public EncounterTableDefinition PreBossTable;
        public EncounterTableDefinition PostBossTable;
        public ZoneDefinition AsterHarborZone;
        public QuestDefinition HarborQuest;
        public DialogueDefinition HarborCaptainDialogue;
        public ShopDefinition GeneralShop;
    }

    internal static class Phase1InstallerShared
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string leaf = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void AssignObjectArray(SerializedProperty property, params Object[] objects)
        {
            property.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
        }

        public static void SetStats(SerializedProperty property, int hp, int mp, int atk, int mag, int def, int res, int speed)
        {
            property.FindPropertyRelative("MaxHP").intValue = hp;
            property.FindPropertyRelative("MaxMP").intValue = mp;
            property.FindPropertyRelative("Attack").intValue = atk;
            property.FindPropertyRelative("Magic").intValue = mag;
            property.FindPropertyRelative("Defense").intValue = def;
            property.FindPropertyRelative("Resistance").intValue = res;
            property.FindPropertyRelative("Speed").intValue = speed;
        }
    }
}
