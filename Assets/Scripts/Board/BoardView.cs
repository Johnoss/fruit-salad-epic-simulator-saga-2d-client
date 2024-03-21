using Node;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace Board
{
    public class BoardView : MonoBehaviour, IInitializable
    {
        [Header("Layout")]
        [SerializeField]
        private GridLayoutGroup _layoutGroup;

        [SerializeField]
        private CanvasScaler _canvasScaler;

        [Impject]
        private BoardConfig _boardConfig;

        [Impject]
        private NodeFactory _nodeFactory;

        [Impject]
        private BoardModel _boardModel;
        
        [Impject]
        private BoardController _boardController;
        
        private int ColumnsCount => _boardConfig.GridResolution.y;
        
        [Impject]
        public void Initialize()
        {
            SetupLayout();
        }

        private void SetupLayout()
        {
            _layoutGroup.constraintCount = ColumnsCount;

            var referenceWidth = _canvasScaler.referenceResolution.x - _boardConfig.WidthPadding * 2;
            var cellSize = Vector2.one * referenceWidth / ColumnsCount;
            _layoutGroup.cellSize = cellSize;

            _boardController.SetNodeSideLength(cellSize.x);
        }
    }
}