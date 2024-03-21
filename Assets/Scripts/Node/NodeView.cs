using Interaction;
using JetBrains.Annotations;
using MVC;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace Node
{
    public class NodeView : AbstractView
    {
        [UsedImplicitly]
        public class ViewFactory : PlaceholderFactory<NodeModel, NodeController, NodeView> { }
        
        [Header("Layout")]
        [SerializeField]
        private RectTransform _tileParent;

        [Header("Debug")]
        [SerializeField]
        private TextMeshProUGUI _coordinatesText;

        [Header("Interaction")]
        [SerializeField]
        private GraphicRaycaster _hitbox;

        private NodeModel _model;
        private InputModel _inputModel;
        
        private NodeController _controller;
        private SelectionController _selectionController;
        
        private string FormattedCoordinates => $"[{_model.Coordinates.x}, {_model.Coordinates.y}]";

        [Impject]
        public void Construct(NodeModel model, NodeController controller, InputModel inputModel, SelectionController selectionController)
        {
            _model = model;
            _controller = controller;
            _inputModel = inputModel;
            _selectionController = selectionController;

            _controller.SetTileParent(_tileParent);

            _coordinatesText.text = FormattedCoordinates;

            gameObject.name = $"Node {FormattedCoordinates}";

            _hitbox.OnPointerEnterAsObservable()
                .Merge(_hitbox.OnPointerDownAsObservable())
                .DelayFrame(1)
                .Where(_ => _inputModel.IsInteracting.Value)
                .Subscribe(_ => _selectionController.TrySelectTile(_model.Coordinates)).AddTo(Disposer);
        }
    }
}