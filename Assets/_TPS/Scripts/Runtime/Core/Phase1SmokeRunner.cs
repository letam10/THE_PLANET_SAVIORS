using System.Collections.Generic;
using System.IO;
using TPS.Runtime.Combat;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;
using TPS.Runtime.World;
using UnityEngine;

namespace TPS.Runtime.Core
{
    public sealed class Phase1SmokeRunner : MonoBehaviour
    {
        private sealed class SmokeSnapshot
        {
            public string SceneName;
            public int Day;
            public int Hour;
            public int Minute;
            public WeatherType Weather;
            public string QuestStatus;
            public string SideQuestStatus;
            public string DialogueVariant;
            public string SideDialogueVariant;
            public string NpcLocation;
            public bool NpcVisible;
            public string EncounterTableId;
            public bool BossCleared;
            public bool DockSuppliesSecured;
            public bool ShopAvailable;
            public int Currency;
            public int ActivePartyCount;
            public string LeadPartySummary;
            public bool SaveExists;
        }

        public static Phase1SmokeRunner Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;
        [SerializeField] private string _questId = "quest_clear_harbor_threat";
        [SerializeField] private string _zoneId = "aster_harbor";
        [SerializeField] private string _npcId = "harbor_captain";
        [SerializeField] private string _dialogueAnchorId = "harbor_captain";
        [SerializeField] private string _merchantId = "harbor_general_store";
        [SerializeField] private string _bossEncounterId = "enc_harbor_captain";
        [SerializeField] private string _sideQuestId = "quest_secure_dock_supplies";
        [SerializeField] private string _sideDialogueAnchorId = "dock_quartermaster";
        [SerializeField] private string _sideZoneFactId = "dock_supplies_secured";

        private readonly List<string> _timeline = new List<string>();
        private SmokeSnapshot _lastSavedSnapshot;
        private string _lastSceneName = string.Empty;
        private string _lastQuestStatus = string.Empty;
        private string _lastSideQuestStatus = string.Empty;
        private string _lastDialogueVariant = string.Empty;
        private string _lastSideDialogueVariant = string.Empty;
        private string _lastNpcLocation = string.Empty;
        private string _lastEncounterTableId = string.Empty;
        private bool _lastNpcVisible;
        private bool _lastBossCleared;
        private bool _lastDockSuppliesSecured;
        private bool _lastShopAvailable;
        private float _nextPollAt;
        private bool _hasSavedSnapshot;

        public IReadOnlyList<string> Timeline => _timeline;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "debug_save.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEventBus.OnEncounterResolved += OnEncounterResolved;
            GameEventBus.OnRewardGranted += OnRewardGranted;
            GameEventBus.OnQuestChanged += OnQuestChanged;
            GameEventBus.OnDialogueStateChanged += OnDialogueChanged;
            GameEventBus.OnWeatherChanged += OnWeatherChanged;
            GameEventBus.OnSleepAdvanced += OnSleepAdvanced;
            GameEventBus.OnGameSaved += OnGameSaved;
            GameEventBus.OnGameLoaded += OnGameLoaded;
            GameEventBus.OnStateResolverCompleted += OnResolverCompleted;
            GameEventBus.OnLevelUp += OnLevelUp;
            GameEventBus.OnPartyChanged += OnPartyChanged;
            GameEventBus.OnProgressionChanged += OnProgressionChanged;
            AddEvent("Smoke runner armed.");
            CaptureDerivedBaselines();
        }

        private void OnDisable()
        {
            GameEventBus.OnEncounterResolved -= OnEncounterResolved;
            GameEventBus.OnRewardGranted -= OnRewardGranted;
            GameEventBus.OnQuestChanged -= OnQuestChanged;
            GameEventBus.OnDialogueStateChanged -= OnDialogueChanged;
            GameEventBus.OnWeatherChanged -= OnWeatherChanged;
            GameEventBus.OnSleepAdvanced -= OnSleepAdvanced;
            GameEventBus.OnGameSaved -= OnGameSaved;
            GameEventBus.OnGameLoaded -= OnGameLoaded;
            GameEventBus.OnStateResolverCompleted -= OnResolverCompleted;
            GameEventBus.OnLevelUp -= OnLevelUp;
            GameEventBus.OnPartyChanged -= OnPartyChanged;
            GameEventBus.OnProgressionChanged -= OnProgressionChanged;
        }

        private void Update()
        {
            if (UnityEngine.Time.unscaledTime < _nextPollAt)
            {
                return;
            }

            _nextPollAt = UnityEngine.Time.unscaledTime + 0.5f;
            TrackDerivedStateChanges();
        }

        public string[] BuildStatusLines()
        {
            SmokeSnapshot snapshot = CaptureSnapshot();
            return new[]
            {
                $"Scene: {snapshot.SceneName}",
                $"Clock: Day {snapshot.Day} {snapshot.Hour:00}:{snapshot.Minute:00} | Weather: {snapshot.Weather}",
                $"Main Quest: {snapshot.QuestStatus} | Dialogue: {snapshot.DialogueVariant}",
                $"Dock Quest: {snapshot.SideQuestStatus} | Dock Dialogue: {snapshot.SideDialogueVariant}",
                $"NPC: {(snapshot.NpcVisible ? "Visible" : "Hidden")} @ {snapshot.NpcLocation}",
                $"Encounter Table: {snapshot.EncounterTableId} | Boss Cleared: {snapshot.BossCleared}",
                $"Dock Supplies Secured: {snapshot.DockSuppliesSecured}",
                $"Shop Available: {snapshot.ShopAvailable} | Currency: {snapshot.Currency}",
                $"Party Active: {snapshot.ActivePartyCount} | {snapshot.LeadPartySummary}",
                $"Save Exists: {snapshot.SaveExists}"
            };
        }

        public void ResetTelemetry()
        {
            _timeline.Clear();
            _hasSavedSnapshot = false;
            CaptureDerivedBaselines();
            AddEvent("Smoke telemetry reset.");
        }

        public void LogManualSnapshot()
        {
            SmokeSnapshot snapshot = CaptureSnapshot();
            AddEvent($"Manual snapshot: {FormatCompactSnapshot(snapshot)}");
        }

        private void OnEncounterResolved(string encounterId, bool victory)
        {
            AddEvent($"Encounter resolved: {encounterId} ({(victory ? "victory" : "failed")})");
        }

        private void OnRewardGranted(string summary)
        {
            AddEvent($"Reward applied: {summary}");
        }

        private void OnQuestChanged(string questId)
        {
            if (questId == _questId || questId == _sideQuestId)
            {
                string label = questId == _sideQuestId ? "Dock quest" : "Quest";
                AddEvent($"{label} state: {GetQuestStatus(questId)}");
            }
        }

        private void OnDialogueChanged(string key)
        {
            if (key == _questId || key == _sideQuestId || key == _dialogueAnchorId || key == _sideDialogueAnchorId || key.StartsWith("dialogue.harbor") || key.StartsWith("dialogue.dock"))
            {
                AddEvent($"Dialogue variants: main={GetDialogueVariantId(_dialogueAnchorId, "dialogue_harbor_captain")}, dock={GetDialogueVariantId(_sideDialogueAnchorId, "dialogue_dock_quartermaster")}");
            }
        }

        private void OnWeatherChanged(WeatherType weatherType)
        {
            AddEvent($"Weather changed to {weatherType}.");
        }

        private void OnSleepAdvanced(int day)
        {
            AddEvent($"Sleep advanced to day {day}. Shops and zone should refresh.");
        }

        private void OnLevelUp(string memberId, int newLevel)
        {
            AddEvent($"{memberId} reached level {newLevel}.");
        }

        private void OnPartyChanged(string memberId)
        {
            if (!string.IsNullOrWhiteSpace(memberId))
            {
                AddEvent($"Party state changed for {memberId}.");
            }
        }

        private void OnProgressionChanged(string memberId)
        {
            if (!string.IsNullOrWhiteSpace(memberId) && PartyService.Instance != null)
            {
                CharacterStatSnapshot snapshot = PartyService.Instance.GetMemberSnapshot(memberId);
                if (snapshot != null)
                {
                    AddEvent($"Progression snapshot {snapshot.DisplayName}: Lv{snapshot.Level} HP {snapshot.Stats.MaxHP} MP {snapshot.Stats.MaxMP}.");
                }
            }
        }

        private void OnGameSaved()
        {
            _lastSavedSnapshot = CaptureSnapshot();
            _hasSavedSnapshot = true;
            AddEvent($"Save snapshot captured: {FormatCompactSnapshot(_lastSavedSnapshot)}");
        }

        private void OnGameLoaded()
        {
            SmokeSnapshot loadedSnapshot = CaptureSnapshot();
            if (!_hasSavedSnapshot)
            {
                AddEvent($"Load completed without prior smoke baseline: {FormatCompactSnapshot(loadedSnapshot)}");
                return;
            }

            string mismatch = CompareSnapshots(_lastSavedSnapshot, loadedSnapshot);
            AddEvent(string.IsNullOrEmpty(mismatch)
                ? "Load restored smoke-critical state correctly."
                : $"Load mismatch detected: {mismatch}");
        }

        private void OnResolverCompleted()
        {
            TrackDerivedStateChanges();
        }

        private void TrackDerivedStateChanges()
        {
            SmokeSnapshot snapshot = CaptureSnapshot();
            if (!string.Equals(snapshot.SceneName, _lastSceneName))
            {
                _lastSceneName = snapshot.SceneName;
                AddEvent($"Scene flow: {snapshot.SceneName}");
            }

            if (!string.Equals(snapshot.QuestStatus, _lastQuestStatus))
            {
                _lastQuestStatus = snapshot.QuestStatus;
                AddEvent($"Quest mirror now {snapshot.QuestStatus}.");
            }

            if (!string.Equals(snapshot.SideQuestStatus, _lastSideQuestStatus))
            {
                _lastSideQuestStatus = snapshot.SideQuestStatus;
                AddEvent($"Dock quest mirror now {snapshot.SideQuestStatus}.");
            }

            if (!string.Equals(snapshot.DialogueVariant, _lastDialogueVariant))
            {
                _lastDialogueVariant = snapshot.DialogueVariant;
                AddEvent($"Dialogue mirror now {snapshot.DialogueVariant}.");
            }

            if (!string.Equals(snapshot.SideDialogueVariant, _lastSideDialogueVariant))
            {
                _lastSideDialogueVariant = snapshot.SideDialogueVariant;
                AddEvent($"Dock dialogue mirror now {snapshot.SideDialogueVariant}.");
            }

            if (!string.Equals(snapshot.NpcLocation, _lastNpcLocation) || snapshot.NpcVisible != _lastNpcVisible)
            {
                _lastNpcLocation = snapshot.NpcLocation;
                _lastNpcVisible = snapshot.NpcVisible;
                AddEvent($"NPC mirror now {(snapshot.NpcVisible ? "visible" : "hidden")} at {snapshot.NpcLocation}.");
            }

            if (!string.Equals(snapshot.EncounterTableId, _lastEncounterTableId))
            {
                _lastEncounterTableId = snapshot.EncounterTableId;
                AddEvent($"Encounter table now {snapshot.EncounterTableId}.");
            }

            if (snapshot.BossCleared != _lastBossCleared)
            {
                _lastBossCleared = snapshot.BossCleared;
                AddEvent($"Boss cleared mirror now {snapshot.BossCleared}.");
            }

            if (snapshot.DockSuppliesSecured != _lastDockSuppliesSecured)
            {
                _lastDockSuppliesSecured = snapshot.DockSuppliesSecured;
                AddEvent($"Dock supplies secured mirror now {snapshot.DockSuppliesSecured}.");
            }

            if (snapshot.ShopAvailable != _lastShopAvailable)
            {
                _lastShopAvailable = snapshot.ShopAvailable;
                AddEvent($"Shop availability now {snapshot.ShopAvailable}.");
            }
        }

        private void CaptureDerivedBaselines()
        {
            SmokeSnapshot snapshot = CaptureSnapshot();
            _lastSceneName = snapshot.SceneName;
            _lastQuestStatus = snapshot.QuestStatus;
            _lastSideQuestStatus = snapshot.SideQuestStatus;
            _lastDialogueVariant = snapshot.DialogueVariant;
            _lastSideDialogueVariant = snapshot.SideDialogueVariant;
            _lastNpcLocation = snapshot.NpcLocation;
            _lastNpcVisible = snapshot.NpcVisible;
            _lastEncounterTableId = snapshot.EncounterTableId;
            _lastBossCleared = snapshot.BossCleared;
            _lastDockSuppliesSecured = snapshot.DockSuppliesSecured;
            _lastShopAvailable = snapshot.ShopAvailable;
        }

        private SmokeSnapshot CaptureSnapshot()
        {
            return new SmokeSnapshot
            {
                SceneName = SceneLoader.Instance != null ? SceneLoader.Instance.CurrentContentScene : "none",
                Day = WorldClock.Instance != null ? WorldClock.Instance.CurrentDay : 0,
                Hour = WorldClock.Instance != null ? WorldClock.Instance.CurrentHour : 0,
                Minute = WorldClock.Instance != null ? WorldClock.Instance.CurrentMinute : 0,
                Weather = WeatherSystem.Instance != null ? WeatherSystem.Instance.CurrentWeather : WeatherType.Sunny,
                QuestStatus = GetQuestStatus(_questId),
                SideQuestStatus = GetQuestStatus(_sideQuestId),
                DialogueVariant = GetDialogueVariantId(_dialogueAnchorId, "dialogue_harbor_captain"),
                SideDialogueVariant = GetDialogueVariantId(_sideDialogueAnchorId, "dialogue_dock_quartermaster"),
                NpcLocation = GameStateManager.Instance != null ? GameStateManager.Instance.GetString($"npc.{_npcId}.location", "unknown") : "unknown",
                NpcVisible = GameStateManager.Instance != null && GameStateManager.Instance.GetBool($"npc.{_npcId}.visible"),
                EncounterTableId = GameStateManager.Instance != null ? GameStateManager.Instance.GetString($"zone.{_zoneId}.encounter_table", "none") : "none",
                BossCleared = EncounterService.Instance != null && EncounterService.Instance.IsEncounterCleared(_bossEncounterId),
                DockSuppliesSecured = ZoneStateService.Instance != null && ZoneStateService.Instance.GetBoolFact(_zoneId, _sideZoneFactId, false),
                ShopAvailable = GameStateManager.Instance != null && GameStateManager.Instance.GetBool($"shop.{_merchantId}.available"),
                Currency = EconomyService.Instance != null ? EconomyService.Instance.Currency : 0,
                ActivePartyCount = PartyService.Instance != null ? PartyService.Instance.GetActiveMemberIds().Count : 0,
                LeadPartySummary = BuildLeadPartySummary(),
                SaveExists = File.Exists(SaveFilePath)
            };
        }

        private string GetQuestStatus(string questId)
        {
            return QuestService.Instance != null ? QuestService.Instance.GetQuestStatus(questId).ToString() : "Unavailable";
        }

        private string GetDialogueVariantId(string anchorId, string fallbackDialogueId)
        {
            if (GameStateManager.Instance != null)
            {
                return GameStateManager.Instance.GetString($"dialogue.{anchorId}.active_variant", "none");
            }

            if (_contentCatalog != null && DialogueStateService.Instance != null)
            {
                DialogueDefinition dialogue = _contentCatalog.GetDialogue(fallbackDialogueId);
                DialogueVariant variant = DialogueStateService.Instance.ResolveCurrentVariant(dialogue);
                return variant != null ? variant.VariantId : "none";
            }

            return "none";
        }

        private void AddEvent(string message)
        {
            _timeline.Add($"{GetClockPrefix()} {message}");
            if (_timeline.Count > 12)
            {
                _timeline.RemoveAt(0);
            }

            Debug.Log($"[Phase1Smoke] {message}");
        }

        private string CompareSnapshots(SmokeSnapshot expected, SmokeSnapshot actual)
        {
            var mismatches = new List<string>();
            CompareField(mismatches, "scene", expected.SceneName, actual.SceneName);
            CompareField(mismatches, "day", expected.Day.ToString(), actual.Day.ToString());
            CompareField(mismatches, "hour", expected.Hour.ToString(), actual.Hour.ToString());
            CompareField(mismatches, "minute", expected.Minute.ToString(), actual.Minute.ToString());
            CompareField(mismatches, "weather", expected.Weather.ToString(), actual.Weather.ToString());
            CompareField(mismatches, "main quest", expected.QuestStatus, actual.QuestStatus);
            CompareField(mismatches, "dock quest", expected.SideQuestStatus, actual.SideQuestStatus);
            CompareField(mismatches, "main dialogue", expected.DialogueVariant, actual.DialogueVariant);
            CompareField(mismatches, "dock dialogue", expected.SideDialogueVariant, actual.SideDialogueVariant);
            CompareField(mismatches, "npc location", expected.NpcLocation, actual.NpcLocation);
            CompareField(mismatches, "npc visibility", expected.NpcVisible.ToString(), actual.NpcVisible.ToString());
            CompareField(mismatches, "encounter table", expected.EncounterTableId, actual.EncounterTableId);
            CompareField(mismatches, "boss cleared", expected.BossCleared.ToString(), actual.BossCleared.ToString());
            CompareField(mismatches, "dock supplies secured", expected.DockSuppliesSecured.ToString(), actual.DockSuppliesSecured.ToString());
            CompareField(mismatches, "shop available", expected.ShopAvailable.ToString(), actual.ShopAvailable.ToString());
            CompareField(mismatches, "currency", expected.Currency.ToString(), actual.Currency.ToString());
            CompareField(mismatches, "active party", expected.ActivePartyCount.ToString(), actual.ActivePartyCount.ToString());
            CompareField(mismatches, "lead party", expected.LeadPartySummary, actual.LeadPartySummary);
            return mismatches.Count == 0 ? string.Empty : string.Join("; ", mismatches);
        }

        private static void CompareField(List<string> mismatches, string label, string expected, string actual)
        {
            if (!string.Equals(expected, actual))
            {
                mismatches.Add($"{label} expected {expected} but got {actual}");
            }
        }

        private static string FormatCompactSnapshot(SmokeSnapshot snapshot)
        {
            return $"{snapshot.SceneName} | main:{snapshot.QuestStatus} | dock:{snapshot.SideQuestStatus} | dlg:{snapshot.DialogueVariant}/{snapshot.SideDialogueVariant} | npc:{snapshot.NpcLocation} | table:{snapshot.EncounterTableId} | dock_fact:{snapshot.DockSuppliesSecured} | cur:{snapshot.Currency} | lead:{snapshot.LeadPartySummary}";
        }

        private static string BuildLeadPartySummary()
        {
            if (PartyService.Instance == null)
            {
                return "Lead: none";
            }

            List<string> activeMembers = PartyService.Instance.GetActiveMemberIds();
            if (activeMembers.Count == 0)
            {
                return "Lead: none";
            }

            CharacterStatSnapshot snapshot = PartyService.Instance.GetMemberSnapshot(activeMembers[0]);
            if (snapshot == null)
            {
                return "Lead: none";
            }

            EquipmentDefinition weapon = snapshot.EquippedWeapon;
            return $"{snapshot.DisplayName} Lv{snapshot.Level} HP {PartyService.Instance.GetCurrentHP(snapshot.CharacterId)}/{snapshot.Stats.MaxHP} MP {PartyService.Instance.GetCurrentMP(snapshot.CharacterId)}/{snapshot.Stats.MaxMP} W:{(weapon != null ? weapon.DisplayName : "None")}";
        }

        private string GetClockPrefix()
        {
            if (WorldClock.Instance == null)
            {
                return "[D-- --:--]";
            }

            return $"[D{WorldClock.Instance.CurrentDay} {WorldClock.Instance.CurrentHour:00}:{WorldClock.Instance.CurrentMinute:00}]";
        }
    }
}
