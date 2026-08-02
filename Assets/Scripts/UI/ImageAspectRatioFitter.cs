using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RevealGame.UI
{
    /// <summary>
    /// Sizes the currently displayed UI image to cover its parent without changing aspect ratio.
    /// Updates are deferred until the Canvas is outside its rebuild passes in both Prefab Mode and play.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ImageAspectRatioFitter : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private RawImage _rawImage;

        private Object _lastSource;
        private Vector2 _lastSourceSize;
        private Vector2 _lastParentSize;
        private bool _hasTrackedSource;
        private Coroutine _deferredRefreshRoutine;

#if UNITY_EDITOR
        private bool _editorRefreshScheduled;
#endif

        private void Reset()
        {
            CacheReferences();
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
            if (_deferredRefreshRoutine != null)
            {
                StopCoroutine(_deferredRefreshRoutine);
                _deferredRefreshRoutine = null;
            }

#if UNITY_EDITOR
            if (_editorRefreshScheduled)
            {
                EditorApplication.update -= RefreshAfterEditorUpdate;
                _editorRefreshScheduled = false;
            }
#endif
        }

        private void OnValidate()
        {
            CacheReferences();
            _hasTrackedSource = false;

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
            CacheReferences();
            if (!TryGetLayoutRects(out var imageRect, out var parentRect)
                || !TryGetSource(out var source, out var sourceSize))
            {
                _hasTrackedSource = false;
                return;
            }

            var parentSize = parentRect.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                _hasTrackedSource = false;
                return;
            }

            var targetSize = CalculateCoverSize(parentSize, sourceSize.x / sourceSize.y);
            var layoutIsCurrent = imageRect.anchorMin == Vector2.zero
                && imageRect.anchorMax == Vector2.one
                && Approximately(imageRect.anchoredPosition, Vector2.zero)
                && Approximately(imageRect.sizeDelta, targetSize - parentSize);

            if (_hasTrackedSource
                && _lastSource == source
                && _lastSourceSize == sourceSize
                && _lastParentSize == parentSize
                && layoutIsCurrent)
                return;

#if UNITY_EDITOR
            // Inspector and prefab validation can overlap a Canvas rebuild. Apply on the next
            // editor update instead, after checking the rebuild registry again.
            if (!Application.isPlaying)
            {
                ScheduleEditorRefresh();
                return;
            }
#endif

            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                ScheduleRefreshAfterCanvasRebuild();
                return;
            }

            ApplyCoverLayout(imageRect, source, sourceSize, parentSize, targetSize);
        }

        private void ApplyCoverLayout(
            RectTransform imageRect,
            Object source,
            Vector2 sourceSize,
            Vector2 parentSize,
            Vector2 targetSize)
        {
            // AspectRatioFitter can change its RectTransform from inside Graphic.DoMeshGeneration
            // during scene load. Drive the equivalent cover geometry here, outside Canvas rebuilds.
            var legacyFitter = imageRect.GetComponent<AspectRatioFitter>();
            if (legacyFitter != null && legacyFitter.enabled)
                legacyFitter.enabled = false;

            if (imageRect.anchorMin != Vector2.zero)
                imageRect.anchorMin = Vector2.zero;
            if (imageRect.anchorMax != Vector2.one)
                imageRect.anchorMax = Vector2.one;
            if (!Approximately(imageRect.anchoredPosition, Vector2.zero))
                imageRect.anchoredPosition = Vector2.zero;

            var targetSizeDelta = targetSize - parentSize;
            if (!Approximately(imageRect.sizeDelta, targetSizeDelta))
                imageRect.sizeDelta = targetSizeDelta;

            _lastSource = source;
            _lastSourceSize = sourceSize;
            _lastParentSize = parentSize;
            _hasTrackedSource = true;
        }

        private static Vector2 CalculateCoverSize(Vector2 parentSize, float sourceAspectRatio)
        {
            var parentAspectRatio = parentSize.x / parentSize.y;
            return sourceAspectRatio >= parentAspectRatio
                ? new Vector2(parentSize.y * sourceAspectRatio, parentSize.y)
                : new Vector2(parentSize.x, parentSize.x / sourceAspectRatio);
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Approximately(left.x, right.x)
                && Mathf.Approximately(left.y, right.y);
        }

#if UNITY_EDITOR
        private void ScheduleEditorRefresh()
        {
            if (_editorRefreshScheduled || !isActiveAndEnabled)
                return;

            _editorRefreshScheduled = true;
            EditorApplication.update += RefreshAfterEditorUpdate;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void RefreshAfterEditorUpdate()
        {
            EditorApplication.update -= RefreshAfterEditorUpdate;
            _editorRefreshScheduled = false;

            if (this == null || !isActiveAndEnabled || Application.isPlaying)
                return;

            CacheReferences();
            if (!TryGetLayoutRects(out var imageRect, out var parentRect)
                || !TryGetSource(out var source, out var sourceSize))
            {
                _hasTrackedSource = false;
                return;
            }

            var parentSize = parentRect.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                _hasTrackedSource = false;
                return;
            }

            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                ScheduleEditorRefresh();
                return;
            }

            var targetSize = CalculateCoverSize(parentSize, sourceSize.x / sourceSize.y);
            ApplyCoverLayout(imageRect, source, sourceSize, parentSize, targetSize);
            EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

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
            if (_image == null)
                _image = GetComponent<Image>();

            if (_rawImage == null)
                _rawImage = GetComponent<RawImage>();
        }

        private bool TryGetLayoutRects(out RectTransform imageRect, out RectTransform parentRect)
        {
            imageRect = transform as RectTransform;
            parentRect = imageRect != null ? imageRect.parent as RectTransform : null;
            return imageRect != null && parentRect != null;
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
