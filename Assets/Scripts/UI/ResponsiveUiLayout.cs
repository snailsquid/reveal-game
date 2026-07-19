using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    [DisallowMultipleComponent]
    public sealed class ResponsiveUiLayout : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float HorizontalPadding = 32f;
        private const float CellSpacing = 20f;

        private CanvasScaler _canvasScaler;
        private GridLayoutGroup _lootboxGrid;
        private RectTransform _lootboxGridRect;
        private Vector2 _lastScreenSize;

        private void Awake()
        {
            _canvasScaler = FindAnyObjectByType<CanvasScaler>();
            ConfigureCanvas();
            RefreshLayout();
        }

        private void Update()
        {
            var currentSize = new Vector2(Screen.width, Screen.height);
            if (currentSize != _lastScreenSize && _lootboxGrid != null && _lootboxGridRect != null)
                RefreshLayout();
        }

        public void RefreshLayout()
        {
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            ConfigureCanvas();

            if (!TryFindLootboxGrid())
                return;

            float width = _lootboxGridRect.rect.width;
            if (width <= 0f)
                return;

            int columns = width < 500f ? 2 : width < 900f ? 3 : 4;
            float availableWidth = width - (HorizontalPadding * 2f) - (CellSpacing * (columns - 1));
            float cellSize = Mathf.Clamp(availableWidth / columns, 112f, 260f);

            _lootboxGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _lootboxGrid.constraintCount = columns;
            _lootboxGrid.spacing = new Vector2(CellSpacing, CellSpacing);
            _lootboxGrid.padding = new RectOffset((int)HorizontalPadding, (int)HorizontalPadding, 16, 16);
            _lootboxGrid.cellSize = new Vector2(cellSize, cellSize * 0.78f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_lootboxGridRect);
        }

        private void ConfigureCanvas()
        {
            if (_canvasScaler == null)
                return;

            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = Screen.width < Screen.height ? 1f : 0.5f;
        }

        private bool TryFindLootboxGrid()
        {
            if (_lootboxGrid != null && _lootboxGridRect != null)
                return true;

            var screen = FindAnyObjectByType<LootboxScreenController>();
            if (screen == null)
                return false;

            var grid = screen.transform.Find("LootboxGrid");
            if (grid == null)
                return false;

            _lootboxGrid = grid.GetComponent<GridLayoutGroup>();
            _lootboxGridRect = grid.GetComponent<RectTransform>();
            return _lootboxGrid != null && _lootboxGridRect != null;
        }
    }
}
