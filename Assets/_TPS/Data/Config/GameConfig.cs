using UnityEngine;

namespace TPS.Data.Config
{
    [CreateAssetMenu(fileName = "CFG_GameConfig", menuName = "TPS/Config/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Boot")]
        [SerializeField] private bool _bootToMainMenu = false;
        [SerializeField] private string _coreSceneName = "Core";
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private string _startingWorldSceneName = "ZN_Town_AsterHarbor";

        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private string _defaultSpawnId = "Default";

        [Header("World Time")]
        [Min(1)][SerializeField] private int _startDay = 1;
        [Range(0, 23)][SerializeField] private int _startHour = 8;
        [Range(0, 59)][SerializeField] private int _startMinute = 0;
        [Min(0.01f)][SerializeField] private float _worldMinutesPerRealSecond = 1f;

        [Header("Weather")]
        [SerializeField] private int _startingWeather = 0;

        public bool BootToMainMenu => _bootToMainMenu;
        public string CoreSceneName => _coreSceneName;
        public string MainMenuSceneName => _mainMenuSceneName;
        public string StartingWorldSceneName => _startingWorldSceneName;

        public GameObject PlayerPrefab => _playerPrefab;
        public string DefaultSpawnId => _defaultSpawnId;

        public int StartDay => _startDay;
        public int StartHour => _startHour;
        public int StartMinute => _startMinute;
        public float WorldMinutesPerRealSecond => _worldMinutesPerRealSecond;

        public int StartingWeather => _startingWeather;
    }
}
