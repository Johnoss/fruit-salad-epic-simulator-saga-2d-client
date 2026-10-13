using MVC;
using Sound;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace UI
{
    public class SoundToggleView : AbstractView, IInitializable
    {
        [Header("Button")]
        [SerializeField]
        private Button _toggleButton;

        [SerializeField]
        [Tooltip("The icon Image inside the button, whose sprite is swapped. Not the button's own " +
                 "graphic - swapping that would replace the button background.")]
        private Image _iconImage;

        [Header("Sprites")]
        [SerializeField]
        private Sprite _soundOnSprite;

        [SerializeField]
        private Sprite _soundOffSprite;

        [Impject]
        private SoundModel _soundModel;

        [Impject]
        private SoundController _soundController;

        [Impject]
        public void Initialize()
        {
            if (_iconImage == null)
            {
                // Deliberately not falling back to _toggleButton.image: that is the button's own
                // background, and swapping a speaker icon into it would read as a broken layout
                // rather than as an unassigned field.
                Debug.LogError("[SoundToggleView] No icon Image assigned, so the toggle cannot show its state.", this);
            }

            _toggleButton.OnClickAsObservable().SubscribeBlind(_soundController.ToggleMute).AddTo(Disposer);

            // Driven by the combined state, not by the player's toggle alone, so the icon always
            // shows whether there is actually any sound - including when the platform muted us.
            _soundModel.IsAudioEnabled.Subscribe(UpdateIcon).AddTo(Disposer);
        }

        private void UpdateIcon(bool isAudioEnabled)
        {
            var sprite = isAudioEnabled ? _soundOnSprite : _soundOffSprite;

            if (_iconImage == null || sprite == null)
            {
                return;
            }

            _iconImage.sprite = sprite;
        }
    }
}
