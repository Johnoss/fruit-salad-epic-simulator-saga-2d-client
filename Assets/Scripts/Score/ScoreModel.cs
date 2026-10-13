using JetBrains.Annotations;
using MVC;
using Save;
using UniRx;
using UnityEngine;

namespace Score
{
    [UsedImplicitly]
    public class ScoreModel : AbstractModel
    {
        private readonly IReactiveProperty<float> _score = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> Score => _score;

        private readonly SaveModel _saveModel;

        public ScoreModel(SaveModel saveModel)
        {
            _saveModel = saveModel;
        }

        private readonly IReactiveProperty<float> _grandTotal = new ReactiveProperty<float>();

        /// <summary>
        /// Every level's score added together. This is the number the player is shown between
        /// levels and the one the platform leaderboard gets - the per-level Score resets, this does
        /// not.
        /// </summary>
        public IReadOnlyReactiveProperty<float> GrandTotal => _grandTotal;

        /// <summary>
        /// Banks a finished level's score. Written straight through to the save, which keeps it
        /// monotonic and is the same field the leaderboard is sent, so the two cannot drift.
        /// </summary>
        public void AddToGrandTotal(float levelScore)
        {
            SetGrandTotal(_grandTotal.Value + levelScore);

            _saveModel.SetGrandTotalScore(_grandTotal.Value);
        }

        /// <summary>
        /// Adopts a stored total. Takes the higher of the two, because a level can finish before a
        /// slow load returns and the score banked in that window must not be thrown away.
        /// </summary>
        public void SetGrandTotal(float grandTotal)
        {
            _grandTotal.Value = Mathf.Max(_grandTotal.Value, grandTotal);
        }

        public void UpdateScore(float deltaScore)
        {
            SetScore(_score.Value + deltaScore);
        }

        public void SetScore(float score)
        {
            _score.Value = score;
        }

    }
}