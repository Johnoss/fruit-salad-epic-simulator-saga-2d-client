using UnityEngine;

namespace UI.Animation
{
    [CreateAssetMenu(menuName = "Tweens/Create PunchTweenConfig", fileName = "PunchTweenConfig", order = 0)]
    public class PunchTweenConfig : AbstractTweenConfig
    {
        [Header("Punch")]
        [SerializeField]
        private Vector3 _punchIntensity;
        [SerializeField]
        private float _elasticity;
        [SerializeField]
        private int _vibrato;

        public Vector3 PunchIntensity => _punchIntensity;
        public int Vibrato => _vibrato;
        public float Elasticity => _elasticity;
    }
}