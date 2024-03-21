using System;
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
        
        [Impject]
        private SoundConfig _soundConfig;

        [Impject]
        private SelectionModel _selectionModel;
        [Impject] private GoalModel _goalModel;
        [Impject] private ScoreModel _scoreModel;

        private CompositeDisposable _oneShotDisposer;


        [Impject]
        public void Initialize()
        {
            _oneShotDisposer = new CompositeDisposable().AddTo(Disposer);
            
            _selectionModel.SelectedTiles.ObserveAdd().SubscribeBlind(PlayTileSelectedClip).AddTo(Disposer);
            _goalModel.GoalReached.WhereTrue().SubscribeBlind(PlayGameOverClip).AddTo(Disposer);
            _selectionModel.OnCollect.Subscribe(PlayCollectClip).AddTo(Disposer);
        }

        [UsedImplicitly]
        public void PlaySplash()
        {
            PlayClip(SoundType.Splash);
        }

        private void PlayClip(SoundType type, float pitch = 1, int audioSourceIndex = 0)
        {
            if (audioSourceIndex > _audioSources.Length)
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
    }
}