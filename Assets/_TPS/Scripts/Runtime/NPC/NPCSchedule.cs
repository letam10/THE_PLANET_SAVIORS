using System;
using System.Collections.Generic;
using UnityEngine;
using TPS.Runtime.Core;
using TPS.Runtime.Conditions;
using TPS.Runtime.Time;

namespace TPS.Runtime.NPC
{
    [Serializable]
    public class NPCScheduleSlot
    {
        [Tooltip("Identifier for debugging")]
        public string SlotName;

        [Header("Time Range")]
        [Range(0, 24)] public int StartHour;
        [Range(0, 24)] public int EndHour;

        [Header("Extra Conditions")]
        public ConditionResolver ExtraConditions = new ConditionResolver();

        [Header("Result Action")]
        public Transform TargetMarker;
        public bool Visible = true;

        public bool IsActive(int currentHour)
        {
            if (currentHour < StartHour || currentHour > EndHour) return false;
            return ExtraConditions.EvaluateAll();
        }
    }

    /// <summary>
    /// Evaluates list of slots and teleports/enables the NPC.
    /// Does not use NavMesh interpolation for Phase 1.
    /// </summary>
    public sealed class NPCSchedule : MonoBehaviour
    {
        [SerializeField] private GameObject _npcModelRoot;
        [SerializeField] private List<NPCScheduleSlot> _slots = new List<NPCScheduleSlot>();

        [Header("Default Fallback")]
        [Tooltip("If no slots match, should the NPC disappear?")]
        [SerializeField] private bool _hideIfNoSlotMatched = true;
        [Tooltip("If not hiding, where to idle if no slot matched. Left empty = stay where they are.")]
        [SerializeField] private Transform _fallbackMarker;

        private void OnEnable()
        {
            GameEventBus.OnHourChanged += OnEvent_Hour;
            GameEventBus.OnGameLoaded += OnEvent_Generic;
            GameEventBus.OnGameStateChanged += OnEvent_State;
            GameEventBus.OnWeatherChanged += OnEvent_Weather;
        }

        private void OnDisable()
        {
            GameEventBus.OnHourChanged -= OnEvent_Hour;
            GameEventBus.OnGameLoaded -= OnEvent_Generic;
            GameEventBus.OnGameStateChanged -= OnEvent_State;
            GameEventBus.OnWeatherChanged -= OnEvent_Weather;
        }

        private void Start()
        {
            EvaluateSchedule();
        }

        private void OnEvent_Hour(int day, int hour) => EvaluateSchedule();
        private void OnEvent_Generic() => EvaluateSchedule();
        private void OnEvent_State(string key) => EvaluateSchedule();
        private void OnEvent_Weather(TPS.Runtime.Weather.WeatherType wt) => EvaluateSchedule();

        [ContextMenu("Evaluate Schedule")]
        public void EvaluateSchedule()
        {
            if (WorldClock.Instance == null) return;
            
            int currentHour = WorldClock.Instance.CurrentHour;
            NPCScheduleSlot matchedSlot = null;

            foreach (var slot in _slots)
            {
                if (slot.IsActive(currentHour))
                {
                    matchedSlot = slot;
                    break;
                }
            }

            if (matchedSlot != null)
            {
                ApplySlot(matchedSlot.TargetMarker, matchedSlot.Visible);
            }
            else
            {
                ApplyFallback();
            }
        }

        private void ApplySlot(Transform marker, bool visible)
        {
            SetModelVisible(visible);

            if (marker != null && visible)
            {
                transform.position = marker.position;
                transform.rotation = marker.rotation;
            }
        }

        private void ApplyFallback()
        {
            if (_hideIfNoSlotMatched)
            {
                SetModelVisible(false);
            }
            else
            {
                SetModelVisible(true);
                if (_fallbackMarker != null)
                {
                    transform.position = _fallbackMarker.position;
                    transform.rotation = _fallbackMarker.rotation;
                }
            }
        }

        private void SetModelVisible(bool visible)
        {
            if (_npcModelRoot != null && _npcModelRoot.activeSelf != visible)
            {
                _npcModelRoot.SetActive(visible);
            }
        }
    }
}
