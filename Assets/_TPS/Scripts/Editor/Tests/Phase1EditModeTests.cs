using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using TPS.Runtime.Time;
using UnityEngine;

namespace TPS.Editor.Tests
{
    public sealed class Phase1EditModeTests
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
            ResetSingleton(typeof(WorldClock));
            ResetSingleton(typeof(InventoryService));
            ResetSingleton(typeof(EconomyService));
            ResetSingleton(typeof(ProgressionService));
            ResetSingleton(typeof(PartyService));
        }

        [Test]
        public void ConditionResolver_TimeRangeSupportsOvernightWindow()
        {
            WorldClock worldClock = CreateComponent<WorldClock>("WorldClock");
            RegisterSingleton(typeof(WorldClock), worldClock);
            worldClock.SetDateTime(1, 23, 0);

            var resolver = new ConditionResolver
            {
                Mode = ConditionGroupMode.All,
                Conditions = new List<GameCondition>
                {
                    new GameCondition
                    {
                        Type = ConditionType.TimeRange,
                        StartHour = 22,
                        EndHour = 2
                    }
                }
            };

            Assert.That(resolver.EvaluateAll(), Is.True);

            worldClock.SetDateTime(1, 15, 0);
            Assert.That(resolver.EvaluateAll(), Is.False);
        }

        [Test]
        public void CombatFormula_WetLightningBeatsWetFire()
        {
            var wetStatuses = new List<CombatStatusRuntimeData>
            {
                new CombatStatusRuntimeData
                {
                    StatusType = CombatStatusType.Wet,
                    RemainingTurns = 2
                }
            };

            DamageFormulaInput lightningInput = CreateBaseDamageInput();
            lightningInput.ElementType = ElementType.Lightning;
            lightningInput.TargetStatuses = wetStatuses;

            DamageFormulaInput fireInput = CreateBaseDamageInput();
            fireInput.ElementType = ElementType.Fire;
            fireInput.TargetStatuses = wetStatuses;

            Random.InitState(12345);
            int lightningDamage = CombatFormula.CalculateDamage(lightningInput, out _, out float lightningMultiplier);
            Random.InitState(12345);
            int fireDamage = CombatFormula.CalculateDamage(fireInput, out _, out float fireMultiplier);

            Assert.That(lightningMultiplier, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(fireMultiplier, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(lightningDamage, Is.GreaterThan(fireDamage));
        }

        [Test]
        public void ProgressionService_BuildFinalStatsAppliesGrowthAndEquipment()
        {
            CharacterArchetypeDefinition archetype = CreateAsset<CharacterArchetypeDefinition>();
            SetPrivateField(archetype, "_baseStats", new StatBlock { MaxHP = 30, MaxMP = 10, Attack = 5, Magic = 4, Defense = 3, Resistance = 2, Speed = 6 });
            SetPrivateField(archetype, "_growthStats", new StatBlock { MaxHP = 2, MaxMP = 1, Attack = 1, Magic = 0, Defense = 1, Resistance = 0, Speed = 1 });

            EquipmentDefinition weapon = CreateAsset<EquipmentDefinition>();
            SetPrivateField(weapon, "_equipmentId", "weapon_test");
            SetPrivateField(weapon, "_slotType", EquipmentSlotType.Weapon);
            SetPrivateField(weapon, "_statBonus", new StatBlock { MaxHP = 5, MaxMP = 0, Attack = 3, Magic = 0, Defense = 0, Resistance = 0, Speed = 0 });

            CharacterDefinition character = CreateAsset<CharacterDefinition>();
            SetPrivateField(character, "_characterId", "hero_test");
            SetPrivateField(character, "_displayName", "Hero");
            SetPrivateField(character, "_archetype", archetype);
            SetPrivateField(character, "_startingLevel", 3);
            SetPrivateField(character, "_startingWeapon", weapon);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");

            StatBlock finalStats = progressionService.BuildFinalStats(character, weapon);

            Assert.That(finalStats.MaxHP, Is.EqualTo(39));
            Assert.That(finalStats.MaxMP, Is.EqualTo(12));
            Assert.That(finalStats.Attack, Is.EqualTo(10));
            Assert.That(finalStats.Defense, Is.EqualTo(5));
            Assert.That(finalStats.Speed, Is.EqualTo(8));
        }

        [Test]
        public void EconomyService_BuyAndSellFlowUpdatesCurrencyAndInventory()
        {
            ItemDefinition potion = CreateAsset<ItemDefinition>();
            SetPrivateField(potion, "_itemId", "item_potion");
            SetPrivateField(potion, "_displayName", "Potion");
            SetPrivateField(potion, "_buyPrice", 30);
            SetPrivateField(potion, "_sellPrice", 15);

            ShopEntryDefinition entry = new ShopEntryDefinition
            {
                Item = potion,
                Stock = 1
            };

            ShopDefinition shop = CreateAsset<ShopDefinition>();
            SetPrivateField(shop, "_shopId", "shop_test");
            SetPrivateField(shop, "_entries", new List<ShopEntryDefinition> { entry });

            Phase1ContentCatalog catalog = CreateAsset<Phase1ContentCatalog>();
            SetPrivateField(catalog, "_startingCurrency", 100);
            SetPrivateField(catalog, "_shops", new List<ShopDefinition> { shop });

            InventoryService inventoryService = CreateComponent<InventoryService>("InventoryService");
            RegisterSingleton(typeof(InventoryService), inventoryService);

            EconomyService economyService = CreateComponent<EconomyService>("EconomyService");
            SetPrivateField(economyService, "_contentCatalog", catalog);
            RegisterSingleton(typeof(EconomyService), economyService);
            economyService.EnsureDefaults();

            Assert.That(economyService.Currency, Is.EqualTo(100));
            Assert.That(economyService.BuyItem(shop, entry), Is.True);
            Assert.That(economyService.Currency, Is.EqualTo(70));
            Assert.That(inventoryService.GetItemCount("item_potion"), Is.EqualTo(1));
            Assert.That(economyService.BuyItem(shop, entry), Is.False);
            Assert.That(economyService.SellItem(potion), Is.True);
            Assert.That(economyService.Currency, Is.EqualTo(85));
        }

        [Test]
        public void SaveData_DefaultsToSchemaVersionTwo()
        {
            var saveData = new SaveData();
            Assert.That(SaveData.CurrentVersion, Is.EqualTo(2));
            Assert.That(saveData.SaveVersion, Is.EqualTo(2));
        }

        [Test]
        public void SaveLoadManager_RejectsUnsupportedSaveVersion()
        {
            SaveLoadManager saveLoadManager = CreateComponent<SaveLoadManager>("SaveLoadManager");
            string savePath = Path.Combine(Application.persistentDataPath, "debug_save.json");
            File.WriteAllText(savePath, JsonUtility.ToJson(new SaveData { SaveVersion = 1 }));

            try
            {
                IEnumerator routine = (IEnumerator)typeof(SaveLoadManager)
                    .GetMethod("LoadRoutine", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(saveLoadManager, null);

                Assert.That(routine, Is.Not.Null);
                Assert.That(routine.MoveNext(), Is.False);
            }
            finally
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
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

        private static DamageFormulaInput CreateBaseDamageInput()
        {
            return new DamageFormulaInput
            {
                DamageKind = DamageKind.Physical,
                ElementType = ElementType.Physical,
                Power = 10,
                AttackScale = 1f,
                MagicScale = 0f,
                Attack = 12,
                Magic = 0,
                Defense = 5,
                Resistance = 4,
                WeaponPower = 3,
                TargetResistance = new ResistanceProfile()
            };
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
