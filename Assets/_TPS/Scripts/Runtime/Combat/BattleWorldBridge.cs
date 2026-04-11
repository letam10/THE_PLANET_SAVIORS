using System.Collections;
using System.Collections.Generic;
using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.Player;
using TPS.Runtime.Spawn;
using TPS.Runtime.UI;
using TPS.Runtime.World;
using UnityEngine;
using System.IO;

namespace TPS.Runtime.Combat
{
    public sealed class BattleWorldBridge : MonoBehaviour
    {
        private sealed class BattleUnitState
        {
            public bool IsPartyMember;
            public string UnitId;
            public string DisplayName;
            public CharacterDefinition CharacterDefinition;
            public EnemyDefinition EnemyDefinition;
            public CharacterStatSnapshot Snapshot;
            public EquipmentDefinition EquippedWeapon;
            public StatBlock Stats;
            public ResistanceProfile ResistanceProfile;
            public List<SkillDefinition> Skills = new List<SkillDefinition>();
            public List<CombatStatusRuntimeData> Statuses = new List<CombatStatusRuntimeData>();
            public int CurrentHP;
            public int CurrentMP;

            public bool IsDefeated => CurrentHP <= 0;
            public int Speed => Stats != null ? Stats.Speed : 0;
        }

        private readonly List<BattleUnitState> _partyUnits = new List<BattleUnitState>();
        private readonly List<BattleUnitState> _enemyUnits = new List<BattleUnitState>();
        private readonly List<BattleUnitState> _turnOrder = new List<BattleUnitState>();
        private readonly List<string> _combatLog = new List<string>();

        private EncounterService.PendingEncounterContext _context;
        private BattleUnitState _currentActor;
        private bool _awaitingPlayerInput;
        private bool _battleEnded;
        private int _turnCount;
        private BattleResult _battleResult;
        private bool _returnTriggered;
        private const string AutoWinFileName = ".phase1_battle_autowin.txt";

        private void Start()
        {
            if (EncounterService.Instance == null || !EncounterService.Instance.TryGetPendingEncounter(out _context))
            {
                AppendLog("No pending encounter. Battle bridge idle.");
                return;
            }

            ToggleWorldControls(false);
            BuildBattleState();
            AdvanceTurn();
        }

        private void OnDisable()
        {
            ToggleWorldControls(true);
        }

        private void OnGUI()
        {
            if (_context == null || _context.EncounterDefinition == null)
            {
                return;
            }

            DrawBattleSummary();

            if (_battleEnded)
            {
                DrawBattleEndPanel();
                return;
            }

            if (_awaitingPlayerInput && _currentActor != null)
            {
                DrawPlayerTurnPanel(_currentActor);
            }
        }

        private void Update()
        {
            string autoWinPath = GetProjectPath(AutoWinFileName);
            if (File.Exists(autoWinPath))
            {
                File.Delete(autoWinPath);
                ForceAutomationVictoryAndReturn();
            }
        }

        private void BuildBattleState()
        {
            _partyUnits.Clear();
            _enemyUnits.Clear();
            _turnOrder.Clear();

            if (PartyService.Instance != null && ProgressionService.Instance != null)
            {
                List<string> activeMemberIds = PartyService.Instance.GetActiveMemberIds();
                for (int i = 0; i < activeMemberIds.Count; i++)
                {
                    string memberId = activeMemberIds[i];
                    CharacterDefinition definition = PartyService.Instance.GetCharacterDefinition(memberId);
                    if (definition == null)
                    {
                        continue;
                    }

                    EquipmentDefinition weapon = PartyService.Instance.GetEquippedWeapon(memberId);
                    CharacterStatSnapshot snapshot = ProgressionService.Instance.BuildCharacterSnapshot(definition, weapon);
                    var unit = new BattleUnitState
                    {
                        IsPartyMember = true,
                        UnitId = memberId,
                        DisplayName = definition.DisplayName,
                        CharacterDefinition = definition,
                        Snapshot = snapshot,
                        EquippedWeapon = weapon,
                        Stats = snapshot != null ? snapshot.Stats.ToStatBlock() : new StatBlock(),
                        ResistanceProfile = snapshot != null ? snapshot.ResistanceProfile : new ResistanceProfile(),
                        Skills = snapshot != null ? new List<SkillDefinition>(snapshot.Skills) : new List<SkillDefinition>(),
                        CurrentHP = PartyService.Instance.GetCurrentHP(memberId),
                        CurrentMP = PartyService.Instance.GetCurrentMP(memberId)
                    };
                    _partyUnits.Add(unit);
                }
            }

            IReadOnlyList<EnemyDefinition> enemies = _context.EncounterDefinition.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyDefinition definition = enemies[i];
                if (definition == null)
                {
                    continue;
                }

                var unit = new BattleUnitState
                {
                    IsPartyMember = false,
                    UnitId = definition.EnemyId,
                    DisplayName = definition.DisplayName,
                    EnemyDefinition = definition,
                    Stats = definition.Stats != null ? definition.Stats.Clone() : new StatBlock(),
                    ResistanceProfile = definition.ResistanceProfile != null ? definition.ResistanceProfile.Clone() : new ResistanceProfile(),
                    CurrentHP = definition.Stats != null ? definition.Stats.MaxHP : 1,
                    CurrentMP = definition.Stats != null ? definition.Stats.MaxMP : 0
                };
                for (int skillIndex = 0; skillIndex < definition.Skills.Count; skillIndex++)
                {
                    SkillDefinition skill = definition.Skills[skillIndex];
                    if (skill != null)
                    {
                        unit.Skills.Add(skill);
                    }
                }

                _enemyUnits.Add(unit);
            }

            AppendLog($"Encounter: {_context.EncounterDefinition.DisplayName}");
        }

        private void AdvanceTurn()
        {
            if (_battleEnded)
            {
                return;
            }

            if (CheckBattleEnd())
            {
                return;
            }

            if (_turnOrder.Count == 0)
            {
                BuildTurnOrder();
            }

            if (_turnOrder.Count == 0)
            {
                EndBattle(false, "No combatants available.");
                return;
            }

            _currentActor = _turnOrder[0];
            _turnOrder.RemoveAt(0);

            if (_currentActor == null || _currentActor.IsDefeated)
            {
                AdvanceTurn();
                return;
            }

            ApplyStatusTicks(_currentActor);
            if (_currentActor.IsDefeated)
            {
                AppendLog($"{_currentActor.DisplayName} falls before acting.");
                if (CheckBattleEnd())
                {
                    return;
                }

                AdvanceTurn();
                return;
            }

            _turnCount++;
            if (_currentActor.IsPartyMember)
            {
                _awaitingPlayerInput = true;
            }
            else
            {
                ExecuteEnemyTurn(_currentActor);
            }
        }

        private void BuildTurnOrder()
        {
            _turnOrder.Clear();
            for (int i = 0; i < _partyUnits.Count; i++)
            {
                if (!_partyUnits[i].IsDefeated)
                {
                    _turnOrder.Add(_partyUnits[i]);
                }
            }

            for (int i = 0; i < _enemyUnits.Count; i++)
            {
                if (!_enemyUnits[i].IsDefeated)
                {
                    _turnOrder.Add(_enemyUnits[i]);
                }
            }

            _turnOrder.Sort((left, right) => right.Speed.CompareTo(left.Speed));
        }

        private void ExecuteEnemyTurn(BattleUnitState enemyActor)
        {
            BattleUnitState target = GetFirstLiving(_partyUnits);
            if (target == null)
            {
                EndBattle(false, "Party wiped out.");
                return;
            }

            SkillDefinition chosenSkill = ChooseBestSkill(enemyActor, _partyUnits);
            ExecuteAction(enemyActor, target, chosenSkill);
        }

        private void ExecutePlayerAttack()
        {
            BattleUnitState target = GetFirstLiving(_enemyUnits);
            ExecuteAction(_currentActor, target, null);
        }

        private void ExecutePlayerSkill(SkillDefinition skillDefinition)
        {
            BattleUnitState target = skillDefinition != null && skillDefinition.TargetType == CombatTargetType.Self
                ? _currentActor
                : (skillDefinition != null && (skillDefinition.TargetType == CombatTargetType.SingleAlly || skillDefinition.TargetType == CombatTargetType.AllAllies)
                    ? GetLowestHealthUnit(_partyUnits)
                    : GetFirstLiving(_enemyUnits));

            ExecuteAction(_currentActor, target, skillDefinition);
        }

        private void ExecutePlayerItem()
        {
            if (InventoryService.Instance == null || Phase1RuntimeHUD.Instance == null)
            {
                return;
            }

            ItemDefinition item = Phase1RuntimeHUD.Instance.FindFirstUsableConsumable();
            if (item == null || !InventoryService.Instance.RemoveItem(item, 1))
            {
                AppendLog("No usable consumable available.");
                _awaitingPlayerInput = false;
                AdvanceTurn();
                return;
            }

            BattleUnitState target = GetLowestHealthUnit(_partyUnits) ?? _currentActor;
            int healedHP = item.RestoreHP;
            int healedMP = item.RestoreMP;
            target.CurrentHP = Mathf.Clamp(target.CurrentHP + healedHP, 0, target.Stats.MaxHP);
            target.CurrentMP = Mathf.Clamp(target.CurrentMP + healedMP, 0, target.Stats.MaxMP);
            AppendLog($"{_currentActor.DisplayName} uses {item.DisplayName} on {target.DisplayName}.");
            FinishAction();
        }

        private void ExecuteAction(BattleUnitState actor, BattleUnitState target, SkillDefinition skillDefinition)
        {
            if (actor == null || target == null)
            {
                FinishAction();
                return;
            }

            if (skillDefinition != null && skillDefinition.ResourceType == ResourceType.MP)
            {
                if (actor.CurrentMP < skillDefinition.ResourceCost)
                {
                    AppendLog($"{actor.DisplayName} lacks MP for {skillDefinition.DisplayName}.");
                    skillDefinition = null;
                }
                else
                {
                    actor.CurrentMP -= skillDefinition.ResourceCost;
                }
            }

            if (skillDefinition != null && skillDefinition.IsHealingSkill)
            {
                int healed = CombatFormula.CalculateHealing(skillDefinition.FlatHealing, actor.Stats.Magic);
                target.CurrentHP = Mathf.Clamp(target.CurrentHP + healed, 0, target.Stats.MaxHP);
                AppendLog($"{actor.DisplayName} casts {skillDefinition.DisplayName} and restores {healed} HP to {target.DisplayName}.");
            }
            else
            {
                var damageInput = new DamageFormulaInput
                {
                    DamageKind = skillDefinition != null ? skillDefinition.DamageKind : DamageKind.Physical,
                    ElementType = skillDefinition != null ? skillDefinition.ElementType : ElementType.Physical,
                    Power = skillDefinition != null ? skillDefinition.Power : 6,
                    AttackScale = skillDefinition != null ? skillDefinition.AttackScale : 1f,
                    MagicScale = skillDefinition != null ? skillDefinition.MagicScale : 0f,
                    Attack = actor.Stats.Attack,
                    Magic = actor.Stats.Magic,
                    Defense = target.Stats.Defense,
                    Resistance = target.Stats.Resistance,
                    WeaponPower = actor.EquippedWeapon != null ? actor.EquippedWeapon.WeaponPower : 0,
                    TargetResistance = target.ResistanceProfile,
                    TargetStatuses = target.Statuses,
                    CritChanceBonus = skillDefinition != null ? skillDefinition.CritChanceBonus : 0f
                };

                int damage = CombatFormula.CalculateDamage(damageInput, out bool critical, out float multiplier);
                target.CurrentHP = Mathf.Max(0, target.CurrentHP - damage);
                string attackName = skillDefinition != null ? skillDefinition.DisplayName : "Attack";
                AppendLog($"{actor.DisplayName} uses {attackName} on {target.DisplayName} for {damage} damage{(critical ? " (CRIT)" : "")}.");
                if (multiplier > 1.01f) AppendLog("Weakness exploited.");
                if (multiplier < 0.99f) AppendLog("Resistance reduced the hit.");

                if (skillDefinition != null)
                {
                    ApplySkillStatuses(skillDefinition, target);
                }

                if (target.CurrentHP <= 0)
                {
                    AppendLog($"{target.DisplayName} is defeated.");
                }
            }

            FinishAction();
        }

        private void FinishAction()
        {
            _awaitingPlayerInput = false;
            ReduceStatusDurations(_currentActor);
            if (!CheckBattleEnd())
            {
                AdvanceTurn();
            }
        }

        private void ApplyStatusTicks(BattleUnitState unit)
        {
            for (int i = unit.Statuses.Count - 1; i >= 0; i--)
            {
                CombatStatusRuntimeData status = unit.Statuses[i];
                if (status == null || status.RemainingTurns <= 0)
                {
                    unit.Statuses.RemoveAt(i);
                    continue;
                }

                int damage = CombatFormula.CalculateStatusTickDamage(status.StatusType, unit.Stats.MaxHP);
                if (damage > 0)
                {
                    unit.CurrentHP = Mathf.Max(0, unit.CurrentHP - damage);
                    AppendLog($"{unit.DisplayName} suffers {damage} damage from {status.StatusType}.");
                }
            }
        }

        private void ReduceStatusDurations(BattleUnitState unit)
        {
            for (int i = unit.Statuses.Count - 1; i >= 0; i--)
            {
                CombatStatusRuntimeData status = unit.Statuses[i];
                if (status == null)
                {
                    unit.Statuses.RemoveAt(i);
                    continue;
                }

                status.RemainingTurns--;
                if (status.RemainingTurns <= 0)
                {
                    unit.Statuses.RemoveAt(i);
                }
            }
        }

        private void ApplySkillStatuses(SkillDefinition skillDefinition, BattleUnitState target)
        {
            IReadOnlyList<StatusApplicationDefinition> statuses = skillDefinition.AppliedStatuses;
            for (int i = 0; i < statuses.Count; i++)
            {
                StatusApplicationDefinition status = statuses[i];
                if (status == null || status.StatusType == CombatStatusType.None || Random.value > status.Chance)
                {
                    continue;
                }

                target.Statuses.RemoveAll(existing => existing != null && existing.StatusType == status.StatusType);
                target.Statuses.Add(new CombatStatusRuntimeData
                {
                    StatusType = status.StatusType,
                    RemainingTurns = status.DurationTurns
                });
                AppendLog($"{target.DisplayName} is afflicted with {status.StatusType}.");
            }
        }

        private SkillDefinition ChooseBestSkill(BattleUnitState actor, List<BattleUnitState> targets)
        {
            SkillDefinition lethalSkill = null;
            SkillDefinition weaknessSkill = null;
            SkillDefinition bestAffordableSkill = null;
            int bestPower = int.MinValue;

            for (int i = 0; i < actor.Skills.Count; i++)
            {
                SkillDefinition skill = actor.Skills[i];
                if (skill == null || skill.ResourceType == ResourceType.MP && actor.CurrentMP < skill.ResourceCost)
                {
                    continue;
                }

                if (skill.IsHealingSkill)
                {
                    BattleUnitState ally = GetLowestHealthUnit(actor.IsPartyMember ? _partyUnits : _enemyUnits);
                    if (ally != null && ally.CurrentHP < ally.Stats.MaxHP / 2)
                    {
                        return skill;
                    }
                }

                BattleUnitState target = GetFirstLiving(targets);
                if (target != null)
                {
                    float multiplier = target.ResistanceProfile != null ? target.ResistanceProfile.GetMultiplier(skill.ElementType) : 1f;
                    if (CombatFormula.HasStatus(target.Statuses, CombatStatusType.Wet) && skill.ElementType == ElementType.Lightning)
                    {
                        multiplier *= 1.5f;
                    }

                    int projectedDamage = Mathf.RoundToInt((skill.Power + actor.Stats.Attack + actor.Stats.Magic) * multiplier);
                    if (target.CurrentHP <= projectedDamage)
                    {
                        lethalSkill = skill;
                    }

                    if (multiplier > 1f && weaknessSkill == null)
                    {
                        weaknessSkill = skill;
                    }
                }

                if (skill.Power > bestPower)
                {
                    bestPower = skill.Power;
                    bestAffordableSkill = skill;
                }
            }

            return lethalSkill ?? weaknessSkill ?? bestAffordableSkill;
        }

        private bool CheckBattleEnd()
        {
            bool anyPartyAlive = HasLiving(_partyUnits);
            bool anyEnemyAlive = HasLiving(_enemyUnits);

            if (!anyEnemyAlive)
            {
                EndBattle(true, "Victory.");
                return true;
            }

            if (!anyPartyAlive)
            {
                EndBattle(false, "Defeat.");
                return true;
            }

            return false;
        }

        private void EndBattle(bool victory, string resultLabel)
        {
            _battleEnded = true;
            _awaitingPlayerInput = false;

            _battleResult = new BattleResult
            {
                EncounterId = _context.EncounterDefinition.EncounterId,
                Victory = victory,
                TurnsTaken = Mathf.Max(1, _turnCount)
            };

            for (int i = 0; i < _partyUnits.Count; i++)
            {
                BattleUnitState unit = _partyUnits[i];
                _battleResult.PartyResults.Add(new BattleParticipantResult
                {
                    UnitId = unit.UnitId,
                    CurrentHP = unit.CurrentHP,
                    CurrentMP = unit.CurrentMP,
                    IsKnockedOut = unit.CurrentHP <= 0
                });

                if (PartyService.Instance != null)
                {
                    PartyService.Instance.SetCurrentResources(unit.UnitId, unit.CurrentHP, unit.CurrentMP, unit.CurrentHP <= 0);
                }
            }

            if (victory && EncounterService.Instance != null)
            {
                if (_context.EncounterDefinition.CountsAsClear)
                {
                    EncounterService.Instance.MarkEncounterCleared(_context.EncounterDefinition);
                    GameEventBus.PublishEncounterCompleted(_context.EncounterDefinition.EncounterId);
                }

                if (RewardService.Instance != null)
                {
                    RewardApplicationResult rewardResult = RewardService.Instance.ApplyRewardTable(
                        _context.EncounterDefinition.RewardTable,
                        PartyService.Instance != null ? PartyService.Instance.GetActiveMemberIds() : null);
                    _battleResult.RewardSummary = rewardResult != null ? rewardResult.Summary : resultLabel;
                }
            }
            else
            {
                _battleResult.RewardSummary = resultLabel;
            }

            AppendLog(resultLabel);
        }

        private void DrawBattleSummary()
        {
            float width = 360f;
            GUI.Box(new Rect(10f, 10f, width, 220f), $"Battle: {_context.EncounterDefinition.DisplayName}");

            float y = 40f;
            GUI.Label(new Rect(20f, y, width - 20f, 20f), "Party");
            y += 20f;
            for (int i = 0; i < _partyUnits.Count; i++)
            {
                BattleUnitState unit = _partyUnits[i];
                GUI.Label(new Rect(20f, y, width - 20f, 20f), $"{unit.DisplayName} HP {unit.CurrentHP}/{unit.Stats.MaxHP} MP {unit.CurrentMP}/{unit.Stats.MaxMP}");
                y += 20f;
            }

            y += 10f;
            GUI.Label(new Rect(20f, y, width - 20f, 20f), "Enemies");
            y += 20f;
            for (int i = 0; i < _enemyUnits.Count; i++)
            {
                BattleUnitState unit = _enemyUnits[i];
                GUI.Label(new Rect(20f, y, width - 20f, 20f), $"{unit.DisplayName} HP {unit.CurrentHP}/{unit.Stats.MaxHP}");
                y += 20f;
            }

            GUI.Box(new Rect(Screen.width - 390f, 10f, 380f, 220f), string.Join("\n", _combatLog));
        }

        private void DrawPlayerTurnPanel(BattleUnitState actor)
        {
            float width = 320f;
            float height = 220f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - height - 20f;
            GUI.Box(new Rect(x, y, width, height), $"{actor.DisplayName} Turn");

            if (GUI.Button(new Rect(x + 20f, y + 35f, width - 40f, 28f), "Attack"))
            {
                ExecutePlayerAttack();
            }

            float buttonY = y + 70f;
            for (int i = 0; i < actor.Skills.Count && i < 3; i++)
            {
                SkillDefinition skill = actor.Skills[i];
                string label = $"{skill.DisplayName} ({skill.ResourceCost} MP)";
                bool canUse = skill.ResourceType != ResourceType.MP || actor.CurrentMP >= skill.ResourceCost;
                GUI.enabled = canUse;
                if (GUI.Button(new Rect(x + 20f, buttonY, width - 40f, 28f), label))
                {
                    ExecutePlayerSkill(skill);
                }
                buttonY += 32f;
                GUI.enabled = true;
            }

            if (GUI.Button(new Rect(x + 20f, y + height - 50f, width - 40f, 28f), "Use Best Consumable"))
            {
                ExecutePlayerItem();
            }
        }

        private void DrawBattleEndPanel()
        {
            float width = 340f;
            float height = 140f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), _battleResult.Victory ? "Victory" : "Defeat");
            GUI.Label(new Rect(x + 20f, y + 35f, width - 40f, 40f), _battleResult.RewardSummary);

            if (GUI.Button(new Rect(x + 20f, y + height - 45f, width - 40f, 28f), "Return To World") && !_returnTriggered)
            {
                _returnTriggered = true;
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.StartCoroutine(ReturnToWorldRoutine());
                }
            }
        }

        private IEnumerator ReturnToWorldRoutine()
        {
            if (_context == null || SceneLoader.Instance == null)
            {
                yield break;
            }

            if (PlayerSpawnSystem.Instance != null)
            {
                PlayerSpawnSystem.Instance.SetPendingSpawnTransform(
                    _context.ReturnPosition,
                    _context.ReturnRotation,
                    _context.ReturnSpawnId);
            }

            yield return SceneLoader.Instance.LoadContentSceneAsync(_context.ReturnSceneName);
            yield return null;

            ToggleWorldControls(true);
            if (EncounterService.Instance != null)
            {
                EncounterService.Instance.ClearPendingEncounter();
            }

            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.ResolveAll();
            }
        }

        private void ToggleWorldControls(bool enabled)
        {
            if (PlayerSpawnSystem.Instance == null || PlayerSpawnSystem.Instance.PlayerInstance == null)
            {
                return;
            }

            PlayerController playerController = PlayerSpawnSystem.Instance.PlayerInstance.GetComponent<PlayerController>();
            PlayerInteractionController interactionController = PlayerSpawnSystem.Instance.PlayerInstance.GetComponent<PlayerInteractionController>();
            if (playerController != null) playerController.enabled = enabled;
            if (interactionController != null) interactionController.enabled = enabled;
        }

        public void ForceAutomationVictoryAndReturn()
        {
            if (_battleEnded)
            {
                if (!_returnTriggered && SceneLoader.Instance != null)
                {
                    _returnTriggered = true;
                    SceneLoader.Instance.StartCoroutine(ReturnToWorldRoutine());
                }

                return;
            }

            EndBattle(true, "Automation Victory.");
            if (!_returnTriggered && SceneLoader.Instance != null)
            {
                _returnTriggered = true;
                SceneLoader.Instance.StartCoroutine(ReturnToWorldRoutine());
            }
        }

        private static string GetProjectPath(string fileName)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            return Path.Combine(projectRoot, fileName);
        }

        private static BattleUnitState GetFirstLiving(List<BattleUnitState> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null && !units[i].IsDefeated)
                {
                    return units[i];
                }
            }

            return null;
        }

        private static BattleUnitState GetLowestHealthUnit(List<BattleUnitState> units)
        {
            BattleUnitState lowest = null;
            float lowestRatio = float.MaxValue;
            for (int i = 0; i < units.Count; i++)
            {
                BattleUnitState unit = units[i];
                if (unit == null || unit.IsDefeated || unit.Stats == null || unit.Stats.MaxHP <= 0)
                {
                    continue;
                }

                float ratio = (float)unit.CurrentHP / unit.Stats.MaxHP;
                if (ratio < lowestRatio)
                {
                    lowestRatio = ratio;
                    lowest = unit;
                }
            }

            return lowest;
        }

        private static bool HasLiving(List<BattleUnitState> units)
        {
            return GetFirstLiving(units) != null;
        }

        private void AppendLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _combatLog.Add(message);
            while (_combatLog.Count > 8)
            {
                _combatLog.RemoveAt(0);
            }
        }
    }
}
