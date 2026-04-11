using System.Collections.Generic;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Runtime.World
{
    public sealed class ZoneStateService : MonoBehaviour
    {
        public static ZoneStateService Instance { get; private set; }

        private readonly Dictionary<string, bool> _boolFacts = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _intFacts = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _stringFacts = new Dictionary<string, string>();

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

        public void SetBoolFact(string zoneId, string factId, bool value)
        {
            string key = BuildKey(zoneId, factId);
            if (_boolFacts.TryGetValue(key, out bool currentValue) && currentValue == value)
            {
                return;
            }

            _boolFacts[key] = value;
            GameEventBus.PublishZoneFactsChanged(key);
        }

        public bool GetBoolFact(string zoneId, string factId, bool defaultValue = false)
        {
            return _boolFacts.TryGetValue(BuildKey(zoneId, factId), out bool value) ? value : defaultValue;
        }

        public void SetIntFact(string zoneId, string factId, int value)
        {
            string key = BuildKey(zoneId, factId);
            if (_intFacts.TryGetValue(key, out int currentValue) && currentValue == value)
            {
                return;
            }

            _intFacts[key] = value;
            GameEventBus.PublishZoneFactsChanged(key);
        }

        public int GetIntFact(string zoneId, string factId, int defaultValue = 0)
        {
            return _intFacts.TryGetValue(BuildKey(zoneId, factId), out int value) ? value : defaultValue;
        }

        public void SetStringFact(string zoneId, string factId, string value)
        {
            string key = BuildKey(zoneId, factId);
            if (_stringFacts.TryGetValue(key, out string currentValue) && currentValue == value)
            {
                return;
            }

            _stringFacts[key] = value;
            GameEventBus.PublishZoneFactsChanged(key);
        }

        public string GetStringFact(string zoneId, string factId, string defaultValue = "")
        {
            return _stringFacts.TryGetValue(BuildKey(zoneId, factId), out string value) ? value : defaultValue;
        }

        public ZoneStateData CaptureState()
        {
            var data = new ZoneStateData();
            foreach (var pair in _boolFacts)
            {
                data.BoolFacts.Add(new BoolMapEntry { Key = pair.Key, Value = pair.Value });
            }

            foreach (var pair in _intFacts)
            {
                data.IntFacts.Add(new IntMapEntry { Key = pair.Key, Value = pair.Value });
            }

            foreach (var pair in _stringFacts)
            {
                data.StringFacts.Add(new StringMapEntry { Key = pair.Key, Value = pair.Value });
            }

            return data;
        }

        public void RestoreState(ZoneStateData data)
        {
            _boolFacts.Clear();
            _intFacts.Clear();
            _stringFacts.Clear();

            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.BoolFacts.Count; i++)
            {
                _boolFacts[data.BoolFacts[i].Key] = data.BoolFacts[i].Value;
            }

            for (int i = 0; i < data.IntFacts.Count; i++)
            {
                _intFacts[data.IntFacts[i].Key] = data.IntFacts[i].Value;
            }

            for (int i = 0; i < data.StringFacts.Count; i++)
            {
                _stringFacts[data.StringFacts[i].Key] = data.StringFacts[i].Value;
            }
        }

        private static string BuildKey(string zoneId, string factId)
        {
            return $"{zoneId}.{factId}";
        }
    }
}
