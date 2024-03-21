using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sound
{
    [CreateAssetMenu(menuName = "Create SoundConfig", fileName = "SoundConfig", order = 0)]
    public class SoundConfig : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField]
        private List<ClipSetting> _clipSettings;

        [Header("Effect Modifiers")]
        [SerializeField]
        [Tooltip("Modifies pitch by chain lenght")]
        private float _selectTilePitchModifier;
        [SerializeField]
        private int _maxChainLenght;

        public int MaxChainLenght => _maxChainLenght;

        public ClipSetting GetClipSettingOrDefault(SoundType type)
        {
            return _clipSettings.FirstOrDefault(setting => setting.SoundType == type);
        }

        public float GetSelectTilePitch(int chainLength)
        {
            return 1 + Mathf.Max(chainLength, MaxChainLenght) * _selectTilePitchModifier;
        }
    }
}