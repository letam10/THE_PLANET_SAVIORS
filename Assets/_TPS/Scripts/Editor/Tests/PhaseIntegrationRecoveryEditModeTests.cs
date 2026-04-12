using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TPS.Data.Config;
using TPS.Runtime.Combat;
using TPS.Runtime.Spawn;
using TPS.Runtime.Time;
using TPS.Runtime.UI;
using TPS.Runtime.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TPS.Editor.Tests
{
    public sealed class PhaseIntegrationRecoveryEditModeTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            RuntimeUiInputState.RestoreGameplayFocus();

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
            ResetSingleton(typeof(PlayerSpawnSystem));
            ResetSingleton(typeof(InventoryService));
            ResetSingleton(typeof(ProgressionService));
            ResetSingleton(typeof(PartyService));
        }

        [Test]
        public void RuntimeUiInputState_ToggleAndEventsStayConsistent()
        {
            var events = new List<RuntimeInputMode>();
            RuntimeUiInputState.OnModeChanged += events.Add;

            try
            {
                RuntimeUiInputState.RestoreGameplayFocus();
                Assert.That(RuntimeUiInputState.CurrentMode, Is.EqualTo(RuntimeInputMode.Gameplay));
                Assert.That(RuntimeUiInputState.IsUiFocused, Is.False);

                RuntimeUiInputState.SetUiFocused(true);
                Assert.That(RuntimeUiInputState.CurrentMode, Is.EqualTo(RuntimeInputMode.UI));
                Assert.That(RuntimeUiInputState.IsUiFocused, Is.True);

                RuntimeUiInputState.ToggleUiFocused();
                Assert.That(RuntimeUiInputState.CurrentMode, Is.EqualTo(RuntimeInputMode.Gameplay));
                Assert.That(RuntimeUiInputState.IsUiFocused, Is.False);

                Assert.That(events, Is.EqualTo(new[] { RuntimeInputMode.UI, RuntimeInputMode.Gameplay }));
            }
            finally
            {
                RuntimeUiInputState.OnModeChanged -= events.Add;
                RuntimeUiInputState.RestoreGameplayFocus();
            }
        }

        [Test]
        public void PartyService_BenchRulePreventsLastActiveMember()
        {
            EquipmentDefinition starterWeapon = CreateWeapon("weapon_party_start");
            CharacterDefinition heroA = CreateCharacter("hero_a", starterWeapon, 32, 10);
            CharacterDefinition heroB = CreateCharacter("hero_b", starterWeapon, 28, 12);

            InventoryService inventoryService = CreateComponent<InventoryService>("InventoryService");
            RegisterSingleton(typeof(InventoryService), inventoryService);
            inventoryService.AddEquipment(starterWeapon, 2);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            RegisterSingleton(typeof(ProgressionService), progressionService);

            PartyService partyService = CreateComponent<PartyService>("PartyService");
            RegisterSingleton(typeof(PartyService), partyService);
            partyService.RecruitMember(heroA, true);
            partyService.RecruitMember(heroB, true);

            Assert.That(partyService.GetActiveMemberIds().Count, Is.EqualTo(2));
            Assert.That(partyService.BenchMember(heroB.CharacterId), Is.True);
            Assert.That(partyService.GetActiveMemberIds().Count, Is.EqualTo(1));
            Assert.That(partyService.BenchMember(heroA.CharacterId), Is.False);
            Assert.That(partyService.SetMemberActiveSlot(heroB.CharacterId, 1), Is.True);
            Assert.That(partyService.GetActiveMemberIds().Count, Is.EqualTo(2));
        }

        [Test]
        public void PlayerSpawnSystem_EnsurePlayerOnValidGroundSnapsFromUnsafeHeight()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _createdObjects.Add(ground);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);

            GameObject playerPrefab = new GameObject("TestPlayerPrefab");
            _createdObjects.Add(playerPrefab);
            playerPrefab.AddComponent<CharacterController>();

            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            _createdObjects.Add(config);
            SetPrivateField(config, "_playerPrefab", playerPrefab);
            SetPrivateField(config, "_defaultSpawnId", "Default");

            PlayerSpawnSystem spawnSystem = CreateComponent<PlayerSpawnSystem>("PlayerSpawnSystem");
            RegisterSingleton(typeof(PlayerSpawnSystem), spawnSystem);
            spawnSystem.Initialize(config);

            spawnSystem.TeleportPlayerExact(new Vector3(0f, 18f, 0f), Quaternion.identity);
            bool corrected = spawnSystem.EnsurePlayerOnValidGround("Default");
            Assert.That(corrected, Is.True);

            Assert.That(spawnSystem.TryGetPlayerTransform(out Vector3 position, out _), Is.True);
            Assert.That(position.y, Is.InRange(1f, 1.2f));
        }

        [Test]
        public void InnAnchor_SleepNowAdvancesDayAndRestoresParty()
        {
            WorldClock worldClock = CreateComponent<WorldClock>("WorldClock");
            RegisterSingleton(typeof(WorldClock), worldClock);
            worldClock.SetDateTime(1, 22, 10);

            EquipmentDefinition starterWeapon = CreateWeapon("weapon_sleep_start");
            CharacterDefinition hero = CreateCharacter("hero_sleep", starterWeapon, 40, 10);

            InventoryService inventoryService = CreateComponent<InventoryService>("InventoryService");
            RegisterSingleton(typeof(InventoryService), inventoryService);
            inventoryService.AddEquipment(starterWeapon, 1);

            ProgressionService progressionService = CreateComponent<ProgressionService>("ProgressionService");
            RegisterSingleton(typeof(ProgressionService), progressionService);

            PartyService partyService = CreateComponent<PartyService>("PartyService");
            RegisterSingleton(typeof(PartyService), partyService);
            partyService.RecruitMember(hero, true);
            partyService.SetCurrentResources(hero.CharacterId, 7, 1, false);

            InnAnchor innAnchor = CreateComponent<InnAnchor>("InnAnchor");
            SetPrivateField(innAnchor, "_wakeHour", 7);
            SetPrivateField(innAnchor, "_wakeMinute", 15);
            innAnchor.SleepNow();

            Assert.That(worldClock.CurrentDay, Is.EqualTo(2));
            Assert.That(worldClock.CurrentHour, Is.EqualTo(7));
            Assert.That(worldClock.CurrentMinute, Is.EqualTo(15));

            CharacterStatSnapshot snapshot = partyService.GetMemberSnapshot(hero.CharacterId);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(partyService.GetCurrentHP(hero.CharacterId), Is.EqualTo(snapshot.Stats.MaxHP));
            Assert.That(partyService.GetCurrentMP(hero.CharacterId), Is.EqualTo(snapshot.Stats.MaxMP));
        }

        [Test]
        public void ExternalAssetImporter_ValidateEntryRejectsNonWhitelistedExtensions()
        {
            Type entryType = FindType("TPS.Editor.ExternalAssetSourceEntry");
            Type toolType = FindType("TPS.Editor.ExternalAssetImporterTool");
            Assert.That(entryType, Is.Not.Null);
            Assert.That(toolType, Is.Not.Null);

            object entry = Activator.CreateInstance(entryType);
            SetProperty(entry, "Id", "test_entry");
            SetProperty(entry, "DownloadUrl", "https://example.com/file.ogg");
            SetProperty(entry, "DestinationAssetPath", "Assets/_TPS/Audio/External/SFX/test.ogg");

            MethodInfo validateEntry = toolType.GetMethod("ValidateEntry", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(validateEntry, Is.Not.Null);

            object[] validArgs = { entry, null };
            bool isValid = (bool)validateEntry.Invoke(null, validArgs);
            Assert.That(isValid, Is.True);

            SetProperty(entry, "DestinationAssetPath", "Assets/_TPS/Audio/External/SFX/test.exe");
            object[] invalidArgs = { entry, null };
            bool isInvalid = (bool)validateEntry.Invoke(null, invalidArgs);
            Assert.That(isInvalid, Is.False);
            Assert.That((string)invalidArgs[1], Does.Contain("not whitelisted"));
        }

        [Test]
        public void BattleWorldBridge_WhenIdleUiActionListIsEmpty()
        {
            BattleWorldBridge bridge = CreateComponent<BattleWorldBridge>("BattleWorldBridge");
            IReadOnlyList<BattleWorldBridge.BattleActionView> actions = bridge.GetAvailableActions();
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions.Count, Is.EqualTo(0));
            Assert.That(bridge.GetTurnOrderLabels(), Is.Empty);
            Assert.That(bridge.GetCombatLogLines(), Is.Empty);
        }

        private CharacterDefinition CreateCharacter(string characterId, EquipmentDefinition startingWeapon, int maxHp, int maxMp)
        {
            CharacterArchetypeDefinition archetype = CreateAsset<CharacterArchetypeDefinition>();
            SetPrivateField(archetype, "_baseStats", new StatBlock
            {
                MaxHP = maxHp,
                MaxMP = maxMp,
                Attack = 5,
                Magic = 3,
                Defense = 4,
                Resistance = 2,
                Speed = 5
            });
            SetPrivateField(archetype, "_growthStats", new StatBlock());

            CharacterDefinition character = CreateAsset<CharacterDefinition>();
            SetPrivateField(character, "_characterId", characterId);
            SetPrivateField(character, "_displayName", characterId);
            SetPrivateField(character, "_archetype", archetype);
            SetPrivateField(character, "_startingLevel", 1);
            SetPrivateField(character, "_startingWeapon", startingWeapon);
            return character;
        }

        private EquipmentDefinition CreateWeapon(string equipmentId)
        {
            EquipmentDefinition weapon = CreateAsset<EquipmentDefinition>();
            SetPrivateField(weapon, "_equipmentId", equipmentId);
            SetPrivateField(weapon, "_displayName", equipmentId);
            SetPrivateField(weapon, "_slotType", EquipmentSlotType.Weapon);
            SetPrivateField(weapon, "_weaponFamily", WeaponFamilyType.Blade);
            SetPrivateField(weapon, "_statBonus", new StatBlock());
            return weapon;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(asset);
            return asset;
        }

        private T CreateComponent<T>(string objectName) where T : Component
        {
            GameObject gameObject = new GameObject(objectName);
            _createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type candidate = assemblies[i].GetType(fullName, false);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Property '{propertyName}' not found on {target.GetType().Name}.");
            property.SetValue(target, value);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void RegisterSingleton(Type type, object instance)
        {
            FieldInfo backingField = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(backingField, Is.Not.Null, $"Singleton backing field missing on {type.Name}.");
            backingField.SetValue(null, instance);
        }

        private static void ResetSingleton(Type type)
        {
            FieldInfo backingField = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            backingField?.SetValue(null, null);
        }
    }
}
