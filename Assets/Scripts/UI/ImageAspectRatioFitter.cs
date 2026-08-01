using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Keeps an <see cref="AspectRatioFitter"/> synchronized with the currently displayed UI image.
    /// Supports both <see cref="Image"/> sprites and <see cref="RawImage"/> textures.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AspectRatioFitter))]
    public sealed class ImageAspectRatioFitter : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private AspectRatioFitter _aspectRatioFitter;

        private Object _lastSource;
        private Vector2 _lastSourceSize;
        private bool _hasTrackedSource;
        private Coroutine _deferredRefreshRoutine;

        private void Reset()
        {
            CacheReferences();
            RefreshAspectRatio();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            RefreshAspectRatio();
        }

        private void OnDisable()
        {
            if (_deferredRefreshRoutine == null)
                return;

            StopCoroutine(_deferredRefreshRoutine);
            _deferredRefreshRoutine = null;
        }

        private void OnValidate()
        {
            CacheReferences();
            _hasTrackedSource = false;

            // Do not change layout from an editor validation callback. Validation can run while
            // the Canvas is rebuilding its graphics, which causes a re-entrant UI rebuild.
            if (Application.isPlaying)
                RefreshAspectRatio();
        }

        private void Update()
        {
            RefreshAspectRatio();
        }

        /// <summary>
        /// Updates the fitter immediately. This is also performed automatically when the image changes.
        /// </summary>
        public void RefreshAspectRatio()
        {
            if (_aspectRatioFitter == null)
                CacheReferences();

            if (_aspectRatioFitter == null || !TryGetSource(out var source, out var sourceSize))
            {
                _hasTrackedSource = false;
                return;
            }

            if (_hasTrackedSource && _lastSource == source && _lastSourceSize == sourceSize)
                return;

            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                ScheduleRefreshAfterCanvasRebuild();
                return;
            }

            _aspectRatioFitter.aspectRatio = sourceSize.x / sourceSize.y;
            _lastSource = source;
            _lastSourceSize = sourceSize;
            _hasTrackedSource = true;
        }

        private void ScheduleRefreshAfterCanvasRebuild()
        {
            if (_deferredRefreshRoutine == null && isActiveAndEnabled)
                _deferredRefreshRoutine = StartCoroutine(RefreshAfterCanvasRebuild());
        }

        private IEnumerator RefreshAfterCanvasRebuild()
        {
            // Resume after the current render pass before changing a RectTransform. The loop also
            // covers a layout pass that starts immediately after the end-of-frame callback.
            yield return new WaitForEndOfFrame();
            while (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
                yield return null;

            _deferredRefreshRoutine = null;
            RefreshAspectRatio();
        }

        private void CacheReferences()
        {
            if (_aspectRatioFitter == null)
                _aspectRatioFitter = GetComponent<AspectRatioFitter>();

            if (_image == null)
                _image = GetComponent<Image>();

            if (_rawImage == null)
                _rawImage = GetComponent<RawImage>();
        }

        private bool TryGetSource(out Object source, out Vector2 sourceSize)
        {
            var sprite = _image != null ? _image.overrideSprite ?? _image.sprite : null;
            if (sprite != null)
            {
                var spriteRect = sprite.rect;
                source = sprite;
                sourceSize = spriteRect.size;
                return sourceSize.x > 0f && sourceSize.y > 0f;
            }

            var texture = _rawImage != null ? _rawImage.texture : null;
            if (texture != null)
            {
                source = texture;
                sourceSize = new Vector2(texture.width, texture.height);
                return sourceSize.x > 0f && sourceSize.y > 0f;
            }

            source = null;
            sourceSize = default;
            return false;
        }
    }
}
