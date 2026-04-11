using System.Collections;
using System.Collections.Generic;
using System.IO;
using TPS.Runtime.Combat;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;
using TPS.Runtime.World;
using UnityEngine;

namespace TPS.Runtime.Core
{
    public sealed class Phase1AutomationDriver : MonoBehaviour
    {
        private const string RuntimeCommandFileName = ".phase1_runtime_command.txt";
        private const string ResultFileName = "Phase1AutomationResult.txt";
        private const string BattleAutoWinFileName = ".phase1_battle_autowin.txt";
        private bool _started;

        private void Start()
        {
            TryStartAutomation();
        }

        private void Update()
        {
            TryStartAutomation();
        }

        private void TryStartAutomation()
        {
            if (_started)
            {
                return;
            }

            string commandPath = FindExistingPath(RuntimeCommandFileName);
            if (string.IsNullOrEmpty(commandPath))
            {
                return;
            }

            string command = File.ReadAllText(commandPath).Trim();
            File.Delete(commandPath);
            if (string.Equals(command, "RUN_PHASE1_SMOKE", System.StringComparison.OrdinalIgnoreCase))
            {
                _started = true;
                StartCoroutine(RunAutomationSmoke());
            }
        }

        private IEnumerator RunAutomationSmoke()
        {
            var report = new List<string>();
            report.Add("Phase 1 automation smoke started.");

            yield return WaitForContentScene("ZN_Town_AsterHarbor", 10f, report);
            LogReport(report, "Booted into town.");

            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.SetWeather(WeatherType.Sunny, true);
            }

            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.ResolveAll();
            }

            yield return WaitForCondition(() => GameStateManager.Instance != null &&
                                               GameStateManager.Instance.GetString("npc.harbor_captain.location") == "MK_Square_01",
                5f,
                report,
                "NPC reached square morning slot.");

            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.SetWeather(WeatherType.Rain, true);
            }

            yield return WaitForCondition(() => GameStateManager.Instance != null &&
                                               GameStateManager.Instance.GetString("npc.harbor_captain.location") == "MK_Tavern_01",
                5f,
                report,
                "NPC reacted to rain and moved indoors.");

            DialogueAnchor dialogueAnchor = FindAnyObjectByType<DialogueAnchor>();
            if (dialogueAnchor != null)
            {
                dialogueAnchor.Interact(null);
            }

            yield return WaitForCondition(() => Quest.QuestService.Instance != null &&
                                               Quest.QuestService.Instance.GetQuestStatus("quest_clear_harbor_threat") == Quest.QuestStatus.Active,
                5f,
                report,
                "Quest accepted.");

            EncounterAnchor bossAnchor = FindBossAnchor();
            if (bossAnchor != null)
            {
                bossAnchor.Interact(null);
            }

            yield return WaitForContentScene("BTL_Standard", 10f, report);

            File.WriteAllText(GetProjectPath(BattleAutoWinFileName), "AUTOWIN");
            LogReport(report, "Requested automated battle victory.");

            yield return WaitForContentScene("ZN_Town_AsterHarbor", 10f, report);
            yield return WaitForCondition(() => EncounterService.Instance != null &&
                                               EncounterService.Instance.IsEncounterCleared("enc_harbor_captain"),
                5f,
                report,
                "Boss clear returned to world.");

            yield return WaitForCondition(() => GameStateManager.Instance != null &&
                                               GameStateManager.Instance.GetString("zone.aster_harbor.encounter_table") == "table_aster_postboss",
                5f,
                report,
                "Encounter table swapped post-boss.");

            dialogueAnchor = FindAnyObjectByType<DialogueAnchor>();
            if (dialogueAnchor != null)
            {
                dialogueAnchor.Interact(null);
            }

            yield return WaitForCondition(() => Quest.QuestService.Instance != null &&
                                               Quest.QuestService.Instance.GetQuestStatus("quest_clear_harbor_threat") == Quest.QuestStatus.Completed,
                5f,
                report,
                "Quest completed after turn-in.");

            MerchantAnchor merchantAnchor = FindAnyObjectByType<MerchantAnchor>();
            ShopDefinition shop = merchantAnchor != null ? merchantAnchor.ShopDefinition : null;
            if (shop != null && EconomyService.Instance != null && shop.Entries.Count > 0)
            {
                EconomyService.Instance.BuyItem(shop, shop.Entries[0]);
                LogReport(report, $"Shop transaction completed. Currency: {EconomyService.Instance.Currency}");
            }

            if (WorldClock.Instance != null)
            {
                WorldClock.Instance.SleepUntilNextDay(7, 0);
            }

            if (EconomyService.Instance != null && WorldClock.Instance != null)
            {
                EconomyService.Instance.RestockDaily(WorldClock.Instance.CurrentDay);
            }

            yield return WaitForCondition(() => WorldClock.Instance != null && WorldClock.Instance.CurrentHour == 7,
                5f,
                report,
                "Sleep advanced to next morning.");

            if (SaveLoad.SaveLoadManager.Instance != null)
            {
                SaveLoad.SaveLoadManager.Instance.SaveGame();
                yield return null;
                SaveLoad.SaveLoadManager.Instance.LoadGame();
            }

            yield return WaitForCondition(() => Phase1SmokeRunner.Instance != null &&
                                               HasTimelineEntry("Load restored smoke-critical state correctly."),
                10f,
                report,
                "Save/load restored smoke-critical state.");

            report.Add("Automation smoke complete.");
            File.WriteAllLines(GetProjectPath(ResultFileName), report);
            Debug.Log("[Phase1Auto] Automation smoke complete.");
        }

        private IEnumerator WaitForContentScene(string sceneName, float timeout, List<string> report)
        {
            yield return WaitForCondition(() => SceneLoader.Instance != null && SceneLoader.Instance.CurrentContentScene == sceneName,
                timeout,
                report,
                $"Scene flow reached {sceneName}.");
        }

        private IEnumerator WaitForCondition(System.Func<bool> predicate, float timeout, List<string> report, string successMessage)
        {
            float endTime = UnityEngine.Time.unscaledTime + timeout;
            while (UnityEngine.Time.unscaledTime < endTime)
            {
                if (predicate())
                {
                    LogReport(report, successMessage);
                    yield break;
                }

                yield return null;
            }

            LogReport(report, $"TIMEOUT: {successMessage}");
        }

        private bool HasTimelineEntry(string fragment)
        {
            IReadOnlyList<string> timeline = Phase1SmokeRunner.Instance.Timeline;
            for (int i = 0; i < timeline.Count; i++)
            {
                if (timeline[i].Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogReport(List<string> report, string message)
        {
            report.Add(message);
            Debug.Log($"[Phase1Auto] {message}");
        }

        private static string GetProjectPath(string fileName)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            return Path.Combine(projectRoot, fileName);
        }

        private static string FindExistingPath(string fileName)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            string[] candidates =
            {
                Path.Combine(projectRoot, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(Application.dataPath, fileName)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return string.Empty;
        }

        private static EncounterAnchor FindBossAnchor()
        {
            EncounterAnchor[] anchors = FindObjectsByType<EncounterAnchor>();
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] != null && anchors[i].name == "ENC_SubBoss_Anchor")
                {
                    return anchors[i];
                }
            }

            return null;
        }
    }
}
