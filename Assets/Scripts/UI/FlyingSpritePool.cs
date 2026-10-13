using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Pools and launches one-shot UI sprites that fly between two screen positions.
    ///
    /// Shared by the fruit goal (board to icon) and the Starfruit reward (icon to board), which
    /// differ only in direction and in how their duration is decided.
    /// </summary>
    public class FlyingSpritePool
    {
        private readonly Image _prefab;
        private readonly RectTransform _parent;
        private readonly Queue<Image> _pooled = new();

        private Canvas _canvas;

        public FlyingSpritePool(Image prefab, RectTransform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        /// False when the prefab or parent is unassigned, so callers can fall back to acting
        /// immediately rather than dropping the effect and whatever it was gating.
        public bool IsUsable => _prefab != null && _parent != null;

        /// <summary>
        /// Distance between two world positions, in screen pixels.
        ///
        /// `RectTransform.position` is only in pixels on a Screen Space Overlay canvas. Under
        /// Screen Space Camera it is world units - a whole screen is a couple of units across
        /// rather than a couple of thousand - so dividing it by a speed in pixels per second gives
        /// a duration close to zero and the flight ends the frame it starts. Converting first keeps
        /// a speed meaning the same thing whatever the canvas render mode is.
        /// </summary>
        public float GetScreenDistance(Vector3 fromWorldPosition, Vector3 toWorldPosition)
        {
            var canvasCamera = GetCanvasCamera();

            var fromScreenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, fromWorldPosition);
            var toScreenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, toWorldPosition);

            return Vector2.Distance(fromScreenPosition, toScreenPosition);
        }

        private Camera GetCanvasCamera()
        {
            if (_canvas == null && _parent != null)
            {
                _canvas = _parent.GetComponentInParent<Canvas>();
            }

            // Overlay canvases take a null camera; the others render through their own.
            return _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
        }

        public void Launch(Sprite sprite, Vector3 fromPosition, Vector3 toPosition, float duration,
            Ease ease, float size, Action onArrived)
        {
            if (!IsUsable)
            {
                onArrived?.Invoke();
                return;
            }

            var flyingSprite = Take();

            if (sprite != null)
            {
                flyingSprite.sprite = sprite;
            }

            var flyingTransform = flyingSprite.rectTransform;

            if (size > 0f)
            {
                flyingTransform.sizeDelta = Vector2.one * size;
            }

            flyingTransform.position = fromPosition;

            flyingTransform
                .DOMove(toPosition, Mathf.Max(0.01f, duration))
                .SetEase(ease)
                .OnComplete(() =>
                {
                    Release(flyingSprite);
                    onArrived?.Invoke();
                });
        }

        private Image Take()
        {
            var flyingSprite = _pooled.Count > 0
                ? _pooled.Dequeue()
                : UnityEngine.Object.Instantiate(_prefab, _parent);

            flyingSprite.transform.SetParent(_parent, false);
            flyingSprite.transform.SetAsLastSibling();
            flyingSprite.gameObject.SetActive(true);

            return flyingSprite;
        }

        private void Release(Image flyingSprite)
        {
            flyingSprite.gameObject.SetActive(false);
            _pooled.Enqueue(flyingSprite);
        }
    }
}
