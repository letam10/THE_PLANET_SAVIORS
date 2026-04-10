using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Editor.Tests
{
    public sealed class Phase23EditModeTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                Object created = _createdObjects[i];
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _createdObjects.Clear();
            ResetSingleton(typeof(InventoryService));
            ResetSingleton(typeof(EconomyService));
            ResetSingleton(typeof(ProgressionService));
            ResetSingleton(typeof(PartyService));
            ResetSingleton(typeof(RewardService));
        }

        [Test]
        public void ProgressionSnapshot_AppliesGrowthEquipmentAndPassiveModifier()
        {
            CharacterArchetypeDefinition archetype = CreateAsset<CharacterArchetypeDefinition>();
            SetPrivateField(archetype, "_baseStats", new StatBlock { MaxHP = 30, MaxMP = 8, Attack = 5, Magic = 3, Defense = 4, Resistance = 2, Speed = 5 });
            SetPrivateField(archetype, "_growthStats", new StatBlock { MaxHP = 2, MaxMP = 1, Attack = 1, Magic = 0, Defense = 1, Resistance = 0, Speed = 1 });
            SetPrivateField(archetype, "_skillUnlocks", new List<SkillUnlockDefinition>
            {
                new SkillUnlockDefinition
                {
                    UnlockId = "unlock_hp_boost",
                    RequiredLevel = 2,
                    PassiveStatModifier = new StatModifier { MaxHP = 6, MaxMP = 4, Attack = 2 },
                    PassiveResistanceModifier = new ResistanceProfile { Physical = 1f, Fire = 0.8f, Ice = 1f, Lightning = 1f }
                }
            });

            EquipmentDefinition weapon = CreateAsset<EquipmentDefinition>();
            SetPrivateField(weapon, "_equipmentId", "weapon_test");
            SetPrivateField(weapon, "_slotType", EquipmentSlotType.Weapon);
            SetPrivateField(weapon, "_statBonus", new StatBlock { MaxHP = 5, MaxMP = 0, Attack = 3, Magic = 0, Defense = 0, Resistance = 0, Speed = 0 });

            CharacterDefinition character = CreateAsset<CharacterDefinition>();
            SetPrivateField(character, "_characterId", "hero_test");
            SetPrivateField(character, "_displayName", "Hero");
            SetPrivateField(character, "_archetype", archetype);
            SetPrivateField(character, "_startingLevel", 2);
            SetPrivateField(character, "_startingWeapon", weapon);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            CharacterStatSnapshot snapshot = progressionService.BuildCharacterSnapshot(character, weapon);

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Stats.MaxHP, Is.EqualTo(43));
            Assert.That(snapshot.Stats.MaxMP, Is.EqualTo(13));
            Assert.That(snapshot.Stats.Attack, Is.EqualTo(11));
            Assert.That(snapshot.ActiveUnlockIds, Does.Contain("unlock_hp_boost"));
            Assert.That(snapshot.ResistanceProfile.Fire, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void PartyService_EquipAndUnequipClampCurrentResources()
        {
            EquipmentDefinition starterWeapon = CreateWeapon("weapon_starter", 0, 0);
            EquipmentDefinition vitalityWeapon = CreateWeapon("weapon_vitality", 20, 0);
            CharacterDefinition character = CreateCharacter("hero_equip", 40, 10, starterWeapon);

            InventoryService inventoryService = CreateComponent<InventoryService>("InventoryService");
            RegisterSingleton(typeof(InventoryService), inventoryService);
            inventoryService.AddEquipment(starterWeapon, 1);
            inventoryService.AddEquipment(vitalityWeapon, 1);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            RegisterSingleton(typeof(ProgressionService), progressionService);

            PartyService partyService = CreateComponent<PartyService>("PartyService");
            Phase1ContentCatalog catalog = CreateAsset<Phase1ContentCatalog>();
            SetPrivateField(catalog, "_characters", new List<CharacterDefinition> { character });
            SetPrivateField(catalog, "_equipment", new List<EquipmentDefinition> { starterWeapon, vitalityWeapon });
            SetPrivateField(partyService, "_contentCatalog", catalog);
            RegisterSingleton(typeof(PartyService), partyService);
            partyService.RecruitMember(character, true);

            Assert.That(partyService.EquipWeapon(character.CharacterId, vitalityWeapon), Is.True);
            partyService.SetCurrentResources(character.CharacterId, 55, 10, false);
            Assert.That(partyService.GetCurrentHP(character.CharacterId), Is.EqualTo(55));

            Assert.That(partyService.UnequipWeapon(character.CharacterId), Is.True);
            CharacterStatSnapshot snapshot = partyService.GetMemberSnapshot(character.CharacterId);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Stats.MaxHP, Is.EqualTo(40));
            Assert.That(partyService.GetCurrentHP(character.CharacterId), Is.EqualTo(40));
        }

        [Test]
        public void PartyService_UseConsumableClampsAgainstComputedSnapshot()
        {
            ItemDefinition potion = CreateItem("item_potion", restoreHp: 30, restoreMp: 6);
            EquipmentDefinition starterWeapon = CreateWeapon("weapon_staff", 0, 6);
            CharacterDefinition character = CreateCharacter("hero_use", 30, 10, starterWeapon);

            InventoryService inventoryService = CreateComponent<InventoryService>("InventoryService");
            RegisterSingleton(typeof(InventoryService), inventoryService);
            inventoryService.AddItem(potion, 1);
            inventoryService.AddEquipment(starterWeapon, 1);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            RegisterSingleton(typeof(ProgressionService), progressionService);

            PartyService partyService = CreateComponent<PartyService>("PartyService");
            RegisterSingleton(typeof(PartyService), partyService);
            partyService.RecruitMember(character, true);
            partyService.SetCurrentResources(character.CharacterId, 10, 2, false);

            Assert.That(partyService.TryUseConsumable(character.CharacterId, potion), Is.True);
            CharacterStatSnapshot snapshot = partyService.GetMemberSnapshot(character.CharacterId);
            Assert.That(partyService.GetCurrentHP(character.CharacterId), Is.EqualTo(snapshot.Stats.MaxHP));
            Assert.That(partyService.GetCurrentMP(character.CharacterId), Is.EqualTo(8));
            Assert.That(inventoryService.GetItemCount(potion.ItemId), Is.EqualTo(0));
        }

        [Test]
        public void RewardService_AppliesCurrencyInventoryAndProgressionOutcome()
        {
            ProgressionCurveDefinition curve = CreateAsset<ProgressionCurveDefinition>();
            SetPrivateField(curve, "_baseExp", 20);
            SetPrivateField(curve, "_linearExp", 10);
            SetPrivateField(curve, "_quadraticExp", 5);

            EquipmentDefinition starterWeapon = CreateWeapon("weapon_reward", 0, 0);
            CharacterArchetypeDefinition archetype = CreateAsset<CharacterArchetypeDefinition>();
            SetPrivateField(archetype, "_baseStats", new StatBlock { MaxHP = 30, MaxMP = 8, Attack = 5, Magic = 3, Defense = 4, Resistance = 2, Speed = 5 });
            SetPrivateField(archetype, "_growthStats", new StatBlock { MaxHP = 0, MaxMP = 0, Attack = 0, Magic = 0, Defense = 0, Resistance = 0, Speed = 0 });
            SetPrivateField(archetype, "_skillUnlocks", new List<SkillUnlockDefinition>
            {
                new SkillUnlockDefinition
                {
                    UnlockId = "unlock_reward_mp",
                    RequiredLevel = 2,
                    PassiveStatModifier = new StatModifier { MaxMP = 5 }
                }
            });

            CharacterDefinition character = CreateAsset<CharacterDefinition>();
            SetPrivateField(character, "_characterId", "hero_reward");
            SetPrivateField(character, "_displayName", "Reward Hero");
            SetPrivateField(character, "_archetype", archetype);
            SetPrivateField(character, "_startingLevel", 1);
            SetPrivateField(character, "_startingWeapon", starterWeapon);

            ItemDefinition potion = CreateItem("reward_potion", restoreHp: 20, restoreMp: 0);
            EquipmentDefinition rewardWeapon = CreateWeapon("weapon_reward_drop", 8, 0);

            Phase1ContentCatalog catalog = CreateAsset<Phase1ContentCatalog>();
            SetPrivateField(catalog, "_progressionCurve", curve);
            SetPrivateField(catalog, "_characters", new List<CharacterDefinition> { character });

            InventoryService inventoryService = CreateComponent<InventoryService>("InventoryService");
            RegisterSingleton(typeof(InventoryService), inventoryService);
            inventoryService.AddEquipment(starterWeapon, 1);

            EconomyService economyService = CreateComponent<EconomyService>("EconomyService");
            RegisterSingleton(typeof(EconomyService), economyService);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            SetPrivateField(progressionService, "_contentCatalog", catalog);
            RegisterSingleton(typeof(ProgressionService), progressionService);

            PartyService partyService = CreateComponent<PartyService>("PartyService");
            RegisterSingleton(typeof(PartyService), partyService);
            partyService.RecruitMember(character, true);

            RewardService rewardService = CreateComponent<RewardService>("RewardService");
            RegisterSingleton(typeof(RewardService), rewardService);

            RewardTableDefinition reward = CreateAsset<RewardTableDefinition>();
            SetPrivateField(reward, "_currencyReward", 45);
            SetPrivateField(reward, "_expReward", 40);
            SetPrivateField(reward, "_guaranteedItems", new List<ItemGrantDefinition> { new ItemGrantDefinition { Item = potion, Amount = 1 } });
            SetPrivateField(reward, "_guaranteedEquipment", new List<EquipmentGrantDefinition> { new EquipmentGrantDefinition { Equipment = rewardWeapon, Amount = 1 } });

            RewardApplicationResult result = rewardService.ApplyRewardTable(reward, partyService.GetActiveMemberIds());
            CharacterStatSnapshot snapshot = partyService.GetMemberSnapshot(character.CharacterId);

            Assert.That(result.CurrencyGranted, Is.EqualTo(45));
            Assert.That(result.ExpGrantedPerMember, Is.EqualTo(40));
            Assert.That(inventoryService.GetItemCount(potion.ItemId), Is.EqualTo(1));
            Assert.That(inventoryService.GetEquipmentCount(rewardWeapon.EquipmentId), Is.EqualTo(1));
            Assert.That(economyService.Currency, Is.EqualTo(45));
            Assert.That(progressionService.GetLevel(character.CharacterId), Is.EqualTo(2));
            Assert.That(snapshot.Stats.MaxMP, Is.EqualTo(13));
        }

        [Test]
        public void PartyRestoreState_ClampsResourcesAgainstLoadedEquipment()
        {
            EquipmentDefinition boostedWeapon = CreateWeapon("weapon_loaded", 12, 4);
            CharacterDefinition character = CreateCharacter("hero_loaded", 30, 10, boostedWeapon);

            Phase1ContentCatalog catalog = CreateAsset<Phase1ContentCatalog>();
            SetPrivateField(catalog, "_characters", new List<CharacterDefinition> { character });
            SetPrivateField(catalog, "_equipment", new List<EquipmentDefinition> { boostedWeapon });

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            RegisterSingleton(typeof(ProgressionService), progressionService);

            PartyService partyService = CreateComponent<PartyService>("PartyService");
            SetPrivateField(partyService, "_contentCatalog", catalog);
            RegisterSingleton(typeof(PartyService), partyService);

            PartyStateData data = new PartyStateData();
            data.Members.Add(new PartyMemberStateData
            {
                CharacterId = character.CharacterId,
                Recruited = true,
                ActiveSlot = 0,
                EquippedWeaponId = boostedWeapon.EquipmentId,
                CurrentHP = 999,
                CurrentMP = 999,
                IsKnockedOut = false
            });

            partyService.RestoreState(data);
            CharacterStatSnapshot snapshot = partyService.GetMemberSnapshot(character.CharacterId);

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Stats.MaxHP, Is.EqualTo(42));
            Assert.That(snapshot.Stats.MaxMP, Is.EqualTo(14));
            Assert.That(partyService.GetCurrentHP(character.CharacterId), Is.EqualTo(42));
            Assert.That(partyService.GetCurrentMP(character.CharacterId), Is.EqualTo(14));
        }

        private CharacterDefinition CreateCharacter(string characterId, int baseHp, int baseMp, EquipmentDefinition startingWeapon)
        {
            CharacterArchetypeDefinition archetype = CreateAsset<CharacterArchetypeDefinition>();
            SetPrivateField(archetype, "_baseStats", new StatBlock { MaxHP = baseHp, MaxMP = baseMp, Attack = 5, Magic = 3, Defense = 4, Resistance = 2, Speed = 5 });
            SetPrivateField(archetype, "_growthStats", new StatBlock { MaxHP = 0, MaxMP = 0, Attack = 0, Magic = 0, Defense = 0, Resistance = 0, Speed = 0 });

            CharacterDefinition character = CreateAsset<CharacterDefinition>();
            SetPrivateField(character, "_characterId", characterId);
            SetPrivateField(character, "_displayName", characterId);
            SetPrivateField(character, "_archetype", archetype);
            SetPrivateField(character, "_startingLevel", 1);
            SetPrivateField(character, "_startingWeapon", startingWeapon);
            return character;
        }

        private EquipmentDefinition CreateWeapon(string equipmentId, int bonusHp, int bonusMp)
        {
            EquipmentDefinition weapon = CreateAsset<EquipmentDefinition>();
            SetPrivateField(weapon, "_equipmentId", equipmentId);
            SetPrivateField(weapon, "_displayName", equipmentId);
            SetPrivateField(weapon, "_slotType", EquipmentSlotType.Weapon);
            SetPrivateField(weapon, "_weaponFamily", WeaponFamilyType.Blade);
            SetPrivateField(weapon, "_statBonus", new StatBlock { MaxHP = bonusHp, MaxMP = bonusMp, Attack = 0, Magic = 0, Defense = 0, Resistance = 0, Speed = 0 });
            return weapon;
        }

        private ItemDefinition CreateItem(string itemId, int restoreHp, int restoreMp)
        {
            ItemDefinition item = CreateAsset<ItemDefinition>();
            SetPrivateField(item, "_itemId", itemId);
            SetPrivateField(item, "_displayName", itemId);
            SetPrivateField(item, "_restoreHP", restoreHp);
            SetPrivateField(item, "_restoreMP", restoreMp);
            return item;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(asset);
            return asset;
        }

        private T CreateComponent<T>(string objectName) where T : Component
        {
            var gameObject = new GameObject(objectName);
            _createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void RegisterSingleton(System.Type type, object instance)
        {
            FieldInfo backingField = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(backingField, Is.Not.Null, $"Singleton backing field was not found on {type.Name}.");
            backingField.SetValue(null, instance);
        }

        private static void ResetSingleton(System.Type type)
        {
            FieldInfo backingField = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            backingField?.SetValue(null, null);
        }
    }
}
