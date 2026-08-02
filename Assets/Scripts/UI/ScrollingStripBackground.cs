using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Displays a wide background sprite as horizontally scrolling, clipped rows.
    /// Even-numbered rows move right and odd-numbered rows move left.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectMask2D))]
    public sealed class ScrollingStripBackground : MonoBehaviour
    {
        private const float StripHeight = 180f;
        private const int InitialOffsetCount = 6;

        // These fixed offsets keep each row visually distinct without adding further configuration.
        private static readonly float[] InitialOffsetFractions =
        {
            0.04f, 0.27f, 0.51f, 0.74f, 0.12f, 0.39f
        };

        [Header("Animation")]
        [SerializeField, Min(0f)] private float _speed = 90f;

        private readonly List<StripRow> _rows = new();

        private RectTransform _viewport;
        private RectTransform _rowsRoot;
        private Sprite _stripSprite;
        private Vector2 _lastViewportSize;

        private void Awake()
        {
            _viewport = (RectTransform)transform;

            var sourceImage = GetComponent<Image>();
            _stripSprite = sourceImage.overrideSprite ?? sourceImage.sprite;
            sourceImage.enabled = false;

            CreateRowsRoot();
        }

        private void Start()
        {
            RebuildRows();
        }

        private void Update()
        {
            var viewportSize = _viewport.rect.size;
            // Canvas scaling can introduce sub-pixel fluctuations. Rebuilding for those would
            // continuously reset the rows before their scrolling motion becomes visible.
            if ((viewportSize - _lastViewportSize).sqrMagnitude > 0.01f)
                RebuildRows();

            if (_rows.Count == 0 || _speed <= 0f)
                return;

            float distance = _speed * Time.unscaledDeltaTime;
            foreach (var row in _rows)
                ScrollRow(row, distance);
        }

        private void OnDestroy()
        {
            if (_rowsRoot != null)
                Destroy(_rowsRoot.gameObject);
        }

        private void CreateRowsRoot()
        {
            var rowsRootObject = new GameObject("Scrolling Rows (Runtime)", typeof(RectTransform));
            _rowsRoot = rowsRootObject.GetComponent<RectTransform>();
            _rowsRoot.SetParent(_viewport, false);
            _rowsRoot.anchorMin = Vector2.zero;
            _rowsRoot.anchorMax = Vector2.one;
            _rowsRoot.offsetMin = Vector2.zero;
            _rowsRoot.offsetMax = Vector2.zero;
            _rowsRoot.SetAsFirstSibling();
        }

        private void RebuildRows()
        {
            _lastViewportSize = _viewport.rect.size;

            if (_stripSprite == null || _lastViewportSize.x <= 0f || _lastViewportSize.y <= 0f)
                return;

            foreach (var row in _rows)
                Destroy(row.rectTransform.gameObject);
            _rows.Clear();

            int rowCount = Mathf.CeilToInt(_lastViewportSize.y / StripHeight);
            float stripWidth = StripHeight * _stripSprite.rect.width / _stripSprite.rect.height;
            float scrollRange = Mathf.Max(1f, stripWidth - _lastViewportSize.x);

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var rowObject = new GameObject($"Scrolling Strip {rowIndex + 1}", typeof(RectTransform), typeof(Image));
                var rowTransform = rowObject.GetComponent<RectTransform>();
                rowTransform.SetParent(_rowsRoot, false);
                rowTransform.anchorMin = new Vector2(0f, 1f);
                rowTransform.anchorMax = new Vector2(0f, 1f);
                rowTransform.pivot = new Vector2(0f, 1f);
                rowTransform.sizeDelta = new Vector2(stripWidth, StripHeight);

                float offsetFraction = InitialOffsetFractions[rowIndex % InitialOffsetCount];
                rowTransform.anchoredPosition = new Vector2(-scrollRange + (scrollRange * offsetFraction), -rowIndex * StripHeight);

                var image = rowObject.GetComponent<Image>();
                image.sprite = _stripSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.raycastTarget = false;

                _rows.Add(new StripRow(rowTransform, rowIndex % 2 == 0));
            }
        }

        private void ScrollRow(StripRow row, float distance)
        {
            float scrollRange = Mathf.Max(1f, row.rectTransform.rect.width - _lastViewportSize.x);
            float minimumX = -scrollRange;
            float normalizedPosition = row.rectTransform.anchoredPosition.x - minimumX;
            float signedDistance = row.movesRight ? distance : -distance;
            float wrappedPosition = minimumX + Mathf.Repeat(normalizedPosition + signedDistance, scrollRange);

            var position = row.rectTransform.anchoredPosition;
            position.x = wrappedPosition;
            row.rectTransform.anchoredPosition = position;
        }

        private readonly struct StripRow
        {
            public readonly RectTransform rectTransform;
            public readonly bool movesRight;

            public StripRow(RectTransform rectTransform, bool movesRight)
            {
                this.rectTransform = rectTransform;
                this.movesRight = movesRight;
            }
        }
    }
}
