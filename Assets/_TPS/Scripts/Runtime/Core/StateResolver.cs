using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Quest;
using TPS.Runtime.Time;
using TPS.Runtime.World;
using UnityEngine;

namespace TPS.Runtime.Core
{
    public interface IStateResolvable
    {
        void ResolveState();
    }

    public sealed class StateResolver : MonoBehaviour
    {
        public static StateResolver Instance { get; private set; }

        private readonly List<IStateResolvable> _resolvables = new List<IStateResolvable>();

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
            GameEventBus.OnHourChanged += OnTimeChanged;
            GameEventBus.OnDayChanged += OnDayChanged;
            GameEventBus.OnWeatherChanged += OnSimpleEvent;
            GameEventBus.OnGameLoaded += OnGameLoaded;
            GameEventBus.OnQuestChanged += OnStateChanged;
            GameEventBus.OnDialogueStateChanged += OnStateChanged;
            GameEventBus.OnPartyChanged += OnStateChanged;
            GameEventBus.OnInventoryChanged += OnStateChanged;
            GameEventBus.OnProgressionChanged += OnStateChanged;
            GameEventBus.OnEncounterResolved += OnEncounterResolved;
            GameEventBus.OnZoneFactsChanged += OnStateChanged;
            GameEventBus.OnEconomyChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEventBus.OnHourChanged -= OnTimeChanged;
            GameEventBus.OnDayChanged -= OnDayChanged;
            GameEventBus.OnWeatherChanged -= OnSimpleEvent;
            GameEventBus.OnGameLoaded -= OnGameLoaded;
            GameEventBus.OnQuestChanged -= OnStateChanged;
            GameEventBus.OnDialogueStateChanged -= OnStateChanged;
            GameEventBus.OnPartyChanged -= OnStateChanged;
            GameEventBus.OnInventoryChanged -= OnStateChanged;
            GameEventBus.OnProgressionChanged -= OnStateChanged;
            GameEventBus.OnEncounterResolved -= OnEncounterResolved;
            GameEventBus.OnZoneFactsChanged -= OnStateChanged;
            GameEventBus.OnEconomyChanged -= OnStateChanged;
        }

        public void Register(IStateResolvable resolvable)
        {
            if (resolvable != null && !_resolvables.Contains(resolvable))
            {
                _resolvables.Add(resolvable);
            }
        }

        public void Unregister(IStateResolvable resolvable)
        {
            if (resolvable != null)
            {
                _resolvables.Remove(resolvable);
            }
        }

        public void ResolveAll()
        {
            if (QuestService.Instance != null)
            {
                QuestService.Instance.RefreshQuestProgress();
            }

            if (EconomyService.Instance != null && WorldClock.Instance != null)
            {
                EconomyService.Instance.RestockDaily(WorldClock.Instance.CurrentDay);
            }

            if (EncounterService.Instance != null)
            {
                EncounterService.Instance.MirrorResolvedStateToGameState();
            }

            for (int i = 0; i < _resolvables.Count; i++)
            {
                _resolvables[i]?.ResolveState();
            }

            GameEventBus.PublishStateResolverCompleted();
        }

        private void OnTimeChanged(int day, int hour)
        {
            ResolveAll();
        }

        private void OnDayChanged(int day)
        {
            ResolveAll();
        }

        private void OnSimpleEvent(TPS.Runtime.Weather.WeatherType weather)
        {
            ResolveAll();
        }

        private void OnGameLoaded()
        {
            ResolveAll();
        }

        private void OnStateChanged(string key)
        {
            ResolveAll();
        }

        private void OnEncounterResolved(string encounterId, bool victory)
        {
            ResolveAll();
        }
    }
}
