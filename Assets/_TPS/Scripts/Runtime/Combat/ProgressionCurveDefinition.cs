using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "CFG_ProgressionCurve", menuName = "TPS/RPG/Progression Curve")]
    public sealed class ProgressionCurveDefinition : ScriptableObject
    {
        [Min(0)] [SerializeField] private int _baseExp = 20;
        [Min(0)] [SerializeField] private int _linearExp = 10;
        [Min(0)] [SerializeField] private int _quadraticExp = 5;

        public int GetRequiredExpForLevel(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            return _baseExp + (_linearExp * safeLevel) + (_quadraticExp * safeLevel * safeLevel);
        }
    }
}
