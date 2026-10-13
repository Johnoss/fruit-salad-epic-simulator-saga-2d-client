using System;
using Boosters;
using DG.Tweening;
using Interaction;
using JetBrains.Annotations;
using MVC;
using Score;
using UniRx;
using UnityEngine;
using Utils;
using Zenject;
using Random = UnityEngine.Random;

namespace Sound
{
    public class SoundView : AbstractView, IInitializable
    {
        [Header("Sound")]
        [SerializeField]
        private AudioSource[] _audioSources;

        [SerializeField]
        private AudioSource _musicAudioSource;

        [SerializeField]
        [Tooltip("Optional dedicated channel for the intro sting, so it can be faded and stopped " +
                 "on its own. Without it the sting shares the effects source, and fading it also " +
                 "ducks anything else playing on that source at the time.")]
        private AudioSource _splashAudioSource;

        [Impject]
        private SoundConfig _soundConfig;

        [Impject]
        private SelectionModel _selectionModel;
        [Impject] private GoalModel _goalModel;
        [Impject] private ScoreModel _scoreModel;

        // Optional so the scene still boots before MonetizationInstaller is wired up. A hard
        // dependency here would fail injection on SoundView and take the whole SceneContext down.
        [InjectOptional] private BoosterModel _boosterModel;
        [InjectOptional] private StarfruitPlacer _starfruitPlacer;

        [SerializeField]
        [Tooltip("Collapses a batch of Starfruit landings into a single sound. Wants to be longer " +
                 "than the spawn stagger plus the flight, or a reward of five sounds five times.")]
        private float _starfruitSoundWindowSeconds = 0.8f;

        private CompositeDisposable _oneShotDisposer;

        private Tween _splashFadeTween;
        private float _effectsVolume = 1f;
        private float _splashVolume = 1f;

        private void Awake()
        {
            // Captured before anything can fade them, so the fade always has a level to restore to.
            _effectsVolume = _audioSources is { Length: > 0 } ? _audioSources[0].volume : 1f;
            _splashVolume = _splashAudioSource != null ? _splashAudioSource.volume : 1f;
        }

        [Impject]
        public void Initialize()
        {
            _oneShotDisposer = new CompositeDisposable().AddTo(Disposer);

            _selectionModel.SelectedTiles.ObserveAdd().SubscribeBlind(PlayTileSelectedClip).AddTo(Disposer);
            // Follows the panel rather than the run ending, so the sting lands with the panel
            // instead of a second ahead of it. Enlarging the bowl during the pause cancels the
            // panel, and this with it - no game over sound over a run that carried on.
            // Off the completion, not off the panel: the panel is also the launch screen now, and
            // opening the game is not a thing to congratulate anyone for.
            _goalModel.IsLevelComplete.WhereTrue().SubscribeBlind(PlayGameOverClip).AddTo(Disposer);
            _selectionModel.OnCollect.Subscribe(PlayCollectClip).AddTo(Disposer);

            SubscribeBoosterClips();
            SubscribeStarfruitClip();
        }

        private void SubscribeBoosterClips()
        {
            if (_boosterModel == null)
            {
                return;
            }

            // One clip per reward. Firing per placed Starfruit stacked five overlapping copies
            // across the spawn stagger, which read as noise rather than as feedback.
            _boosterModel.OnBoosterGranted.Subscribe(PlayBoosterGrantedClip).AddTo(Disposer);
        }

        private void SubscribeStarfruitClip()
        {
            if (_starfruitPlacer == null)
            {
                return;
            }

            // Driven by the landing rather than the grant, so it lands with the tile the player can
            // see. ThrottleFirst keeps a batch to one sound without the placer having to know that
            // the batch is a batch.
            _starfruitPlacer.OnStarfruitLanded
                .ThrottleFirst(TimeSpan.FromSeconds(Mathf.Max(0f, _starfruitSoundWindowSeconds)))
                .SubscribeBlind(PlayStarfruitClip)
                .AddTo(Disposer);
        }

        [UsedImplicitly]
        public void PlaySplash()
        {
            if (_splashAudioSource == null)
            {
                PlayClip(SoundType.Splash);
                return;
            }

            var setting = _soundConfig.GetClipSettingOrDefault(SoundType.Splash);
            if (setting == null)
            {
                return;
            }

            // Played as a normal clip rather than a one-shot: a one-shot cannot be stopped or
            // faded on its own, which is exactly what skipping the intro needs to do.
            _splashFadeTween?.Kill();
            _splashAudioSource.volume = _splashVolume;
            _splashAudioSource.clip = setting.Clips.RandomElement();
            _splashAudioSource.Play();
        }

        /// <summary>
        /// Fades the intro sting out and stops it. Safe to call when nothing is playing, and safe
        /// to call more than once.
        /// </summary>
        public void FadeOutSplash(float fadeSeconds)
        {
            var hasDedicatedSource = _splashAudioSource != null;
            var source = hasDedicatedSource ? _splashAudioSource : _audioSources[0];
            var restoreVolume = hasDedicatedSource ? _splashVolume : _effectsVolume;

            _splashFadeTween?.Kill();

            if (fadeSeconds <= 0f)
            {
                StopAndRestore(source, restoreVolume);
                return;
            }

            _splashFadeTween = source
                .DOFade(0f, fadeSeconds)
                .SetUpdate(true)
                .OnComplete(() => StopAndRestore(source, restoreVolume));
        }

        private static void StopAndRestore(AudioSource source, float restoreVolume)
        {
            source.Stop();

            // Restore the level, or every later sound on this source stays silent.
            source.volume = restoreVolume;
        }

        private void PlayClip(SoundType type, float pitch = 1, int audioSourceIndex = 0)
        {
            // Was ">", which let an index one past the end through to throw on the array access.
            if (audioSourceIndex >= _audioSources.Length)
            {
                audioSourceIndex = 0;
            }

            var setting = _soundConfig.GetClipSettingOrDefault(type);
            var clip = setting.Clips.RandomElement();
            var delay = Random.Range(0, setting.RandomDelaySeconds);

            _audioSources[audioSourceIndex].pitch = pitch + Random.Range(-setting.RandomPitchOffset, setting.RandomPitchOffset);

            if (delay > 0)
            {
                Observable
                    .Timer(TimeSpan.FromSeconds(delay))
                    .First()
                    .Subscribe(_ => PlayOneShot(audioSourceIndex, clip)).AddTo(_oneShotDisposer);
            }
            else
            {
                PlayOneShot(audioSourceIndex, clip);
            }

        }

        private void PlayOneShot(int audioSourceIndex, AudioClip clip)
        {
            _audioSources[audioSourceIndex].PlayOneShot(clip);
        }

        private void PlayCollectClip(int collectCount)
        {
            _oneShotDisposer.Clear();
            for (var i = 0; i < Mathf.Max(collectCount, _soundConfig.MaxChainLenght); i++)
            {
                PlayClip(SoundType.Plop, 1, 1);
            }
        }

        private void PlayTileSelectedClip()
        {
            var chainLength = _selectionModel.SelectedTiles.Count;
            var effectPitch = _soundConfig.GetSelectTilePitch(chainLength);
            PlayClip(SoundType.Ting, effectPitch);
        }

        private void PlayGameOverClip()
        {
            PlayClip(SoundType.Tada);
        }

        private void PlayBoosterGrantedClip(BoosterType boosterType)
        {
            // Starfruit is not here: it plays on landing instead, so the sound matches the tile
            // appearing rather than the grant that scheduled it half a second earlier.
            if (boosterType == BoosterType.BiggerBowl)
            {
                PlayClip(SoundType.BiggerBowl);
            }
        }

        private void PlayStarfruitClip()
        {
            PlayClip(SoundType.Starfruit);
        }

        public override void Dispose()
        {
            _splashFadeTween?.Kill();

            base.Dispose();
        }
    }
}
