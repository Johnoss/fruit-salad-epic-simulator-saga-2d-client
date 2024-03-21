using System;
using UnityEngine;

namespace Sound
{
    [Serializable]
    public class ClipSetting
    {
        public SoundType SoundType;
        public AudioClip[] Clips;
        public float RandomPitchOffset;
        public float RandomDelaySeconds;
    }
}