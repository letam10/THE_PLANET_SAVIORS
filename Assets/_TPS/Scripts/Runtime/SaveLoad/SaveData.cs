using System;
using TPS.Runtime.Weather;
using UnityEngine;

namespace TPS.Runtime.SaveLoad
{
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;

        public int SaveVersion = CurrentVersion;
        public string CurrentSceneName = "";

        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;

        public int WorldDay = 1;
        public int WorldHour = 8;
        public int WorldMinute = 0;

        public WeatherType CurrentWeather = WeatherType.Sunny;

        public DialogueStateData DialogueState = new DialogueStateData();
        public QuestStateData QuestState = new QuestStateData();
        public PartyStateData PartyState = new PartyStateData();
        public InventoryStateData InventoryState = new InventoryStateData();
        public ProgressionStateData ProgressionState = new ProgressionStateData();
        public EncounterStateData EncounterState = new EncounterStateData();
        public ZoneStateData ZoneState = new ZoneStateData();
        public EconomyStateData EconomyState = new EconomyStateData();
    }
}
