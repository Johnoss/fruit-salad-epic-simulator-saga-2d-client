using DG.Tweening;
using UnityEngine;

namespace UI.Animation
{
    public abstract class AbstractTweenConfig : ScriptableObject
    {
        [Header("Tween Setting")]
        [SerializeField]
        [Tooltip("Duration (in seconds) or speed (in unity units) when speed based is checked")]
        private float _durationSeconds = .5f;
        [SerializeField]
        private float _randomDurationDeviation = 0;
        [SerializeField]
        private Ease _ease = Ease.InSine;
        [SerializeField]
        private bool _isSpeedBased;

        public float DurationSeconds =>
            _durationSeconds + Random.Range(-_randomDurationDeviation, _randomDurationDeviation);
        public Ease Ease => _ease;
        public bool IsSpeedBased => _isSpeedBased;

        [Header("Loops")]
        [SerializeField]
        private int _loops = 1;
        [SerializeField]
        private LoopType _loopType = LoopType.Yoyo;


        public LoopType LoopType => _loopType;
        public int Loops => _loops;

    }
}