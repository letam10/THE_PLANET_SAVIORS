using UnityEngine;
using TPS.Runtime.Core;

namespace TPS.Runtime.Conditions
{
    public enum ActivatorTargetMode
    {
        GameObjectSetActive,
        RendererEnable,
        ColliderEnable
    }

    /// <summary>
    /// Utility component to toggle a GameObject or its components based on global conditions.
    /// Connects the ConditionResolver to tangible scene results.
    /// </summary>
    public sealed class ConditionalActivator : MonoBehaviour, IStateResolvable
    {
        [Tooltip("The conditions to evaluate. Empty means always true.")]
        [SerializeField] private ConditionResolver _resolver = new ConditionResolver();

        [Header("Target Configurations")]
        [SerializeField] private ActivatorTargetMode _targetMode = ActivatorTargetMode.GameObjectSetActive;
        [Tooltip("Target object. If null, relies on components on THIS GameObject.")]
        [SerializeField] private GameObject _targetGameObject;
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private Collider _targetCollider;

        [Header("Invert Result?")]
        [SerializeField] private bool _invertResult = false;

        private void OnEnable()
        {
            // Subscribe to cross-domain events
            GameEventBus.OnHourChanged += OnEvent_Time;
            GameEventBus.OnWeatherChanged += OnEvent_Weather;
            GameEventBus.OnGameStateChanged += OnEvent_StateChanged;
            GameEventBus.OnGameLoaded += OnEvent_GameLoaded;

            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.Register(this);
            }

            // Optional: Evaluate immediately on enable if you don't wait for load.
            // But usually we wait for OnGameLoaded or invoke it manually.
            // Safe to evaluate on awake/enable, but systems might not be initialized.
        }

        private void OnDisable()
        {
            GameEventBus.OnHourChanged -= OnEvent_Time;
            GameEventBus.OnWeatherChanged -= OnEvent_Weather;
            GameEventBus.OnGameStateChanged -= OnEvent_StateChanged;
            GameEventBus.OnGameLoaded -= OnEvent_GameLoaded;

            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.Unregister(this);
            }
        }

        private void Start()
        {
            // First evaluation (if Scene loads normally without SaveLoad)
            EvaluateAndApply();
        }

        private void OnEvent_Time(int day, int hour) => EvaluateAndApply();
        private void OnEvent_Weather(TPS.Runtime.Weather.WeatherType wt) => EvaluateAndApply();
        private void OnEvent_StateChanged(string key) => EvaluateAndApply();
        private void OnEvent_GameLoaded() => EvaluateAndApply();
        public void ResolveState() => EvaluateAndApply();

        [ContextMenu("Evaluate Now")]
        public void EvaluateAndApply()
        {
            bool isConditionMet = _resolver.EvaluateAll();
            bool finalState = _invertResult ? !isConditionMet : isConditionMet;

            switch (_targetMode)
            {
                case ActivatorTargetMode.GameObjectSetActive:
                    GameObject targetObj = _targetGameObject != null ? _targetGameObject : gameObject;
                    if (targetObj.activeSelf != finalState)
                    {
                        targetObj.SetActive(finalState);
                    }
                    break;

                case ActivatorTargetMode.RendererEnable:
                    Renderer rend = _targetRenderer;
                    if (rend == null) rend = GetComponent<Renderer>();
                    if (rend != null && rend.enabled != finalState)
                    {
                        rend.enabled = finalState;
                    }
                    break;

                case ActivatorTargetMode.ColliderEnable:
                    Collider col = _targetCollider;
                    if (col == null) col = GetComponent<Collider>();
                    if (col != null && col.enabled != finalState)
                    {
                        col.enabled = finalState;
                    }
                    break;
            }
        }
    }
}
