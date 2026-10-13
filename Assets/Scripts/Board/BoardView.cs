using MVC;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace Board
{
    public class BoardView : AbstractView, IInitializable
    {
        [Header("Layout")]
        [SerializeField]
        private GridLayoutGroup _layoutGroup;

        [SerializeField]
        private CanvasScaler _canvasScaler;

        [SerializeField]
        [Tooltip("Tallest the board may get, in reference-resolution units. Cells are square and " +
                 "sized off the width, so without a cap a board with more rows than columns runs off " +
                 "the bottom. Zero means square: never taller than it is wide.")]
        private float _maxBoardHeight;

        [Impject]
        private BoardConfig _boardConfig;

        [Impject]
        private BoardModel _boardModel;

        [Impject]
        private BoardController _boardController;

        [Impject]
        public void Initialize()
        {
            // Driven off the model rather than read once, because the board is rebuilt at the
            // level's size and the layout has to follow it.
            _boardModel.GridResolution.Subscribe(SetupLayout).AddTo(Disposer);
        }

        private void SetupLayout(Vector2Int gridResolution)
        {
            if (gridResolution.x <= 0 || gridResolution.y <= 0)
            {
                return;
            }

            _layoutGroup.constraintCount = gridResolution.y;

            var referenceWidth = _canvasScaler.referenceResolution.x - _boardConfig.WidthPadding * 2;
            var cellLength = referenceWidth / gridResolution.y;

            // Cells are square, so rows decide the height. Unset, the cap is the width the board
            // already had: every level then fits the space a square board fits, whatever its shape,
            // and a narrow or letterboxed one gets smaller cells rather than overrunning the UI
            // above and below it.
            var maxBoardHeight = _maxBoardHeight > 0f ? _maxBoardHeight : referenceWidth;

            cellLength = Mathf.Min(cellLength, maxBoardHeight / gridResolution.x);

            _layoutGroup.cellSize = Vector2.one * cellLength;

            _boardController.SetNodeSideLength(cellLength);
        }
    }
}
