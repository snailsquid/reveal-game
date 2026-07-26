using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Shared runtime UI for showing a list of item rewards before or after a lootbox opens.
    /// The item rows are created from the imported game data so the view works with any
    /// number of rewards without requiring one serialized UI object per item.
    /// </summary>
    public abstract class ItemPreviewView : MonoBehaviour, IItemPreviewView
    {
        [Header("Preview Settings")]
        [SerializeField] private string _title = "Rewards";
        [SerializeField] private string _continueLabel = "Continue";
        [SerializeField] private string _previousLabel = "Back";
        [SerializeField] private Font _font;

        private GameManager _gameManager;
        private Text _titleText;
        private Text _emptyText;
        private Transform _itemsContainer;
        private Button _continueButton;
        private Button _previousButton;
        private GameObject _contentRoot;
        private readonly List<GameObject> _itemRows = new List<GameObject>();

        public event Action NextRequested;
        public event Action PreviousRequested;

        protected abstract Color PanelColor { get; }
        protected abstract Color AccentColor { get; }

        protected virtual void Awake()
        {
            BuildUi();
            SetVisible(false);
        }

        public void Setup(GameManager gameManager)
        {
            _gameManager = gameManager;
            BuildUi();
        }

        public void Show(IReadOnlyList<Data.ItemData> items)
        {
            BuildUi();
            ClearRows();

            _titleText.text = _title;
            bool hasItems = items != null && items.Count > 0;
            _emptyText.gameObject.SetActive(!hasItems);

            if (hasItems)
            {
                foreach (var item in items)
                    CreateItemRow(item);
            }

            SetVisible(true);
        }

        public void Clear()
        {
            if (_itemsContainer == null)
                return;

            ClearRows();
            if (_emptyText != null)
                _emptyText.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (visible && !gameObject.activeSelf)
                gameObject.SetActive(true);

            if (!visible && gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void SetPreviousInteractable(bool interactable)
        {
            if (_previousButton != null)
                _previousButton.interactable = interactable;
        }

        private void BuildUi()
        {
            if (_contentRoot != null)
                return;

            _contentRoot = new GameObject("PreviewPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _contentRoot.transform.SetParent(transform, false);
            var panelRect = (RectTransform)_contentRoot.transform;
            panelRect.anchorMin = new Vector2(0.08f, 0.08f);
            panelRect.anchorMax = new Vector2(0.92f, 0.92f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            _contentRoot.GetComponent<Image>().color = PanelColor;

            _titleText = CreateText("PreviewTitle", _contentRoot.transform, 30, TextAnchor.UpperCenter);
            var titleRect = (RectTransform)_titleText.transform;
            titleRect.anchorMin = new Vector2(0.05f, 0.84f);
            titleRect.anchorMax = new Vector2(0.95f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            _titleText.color = AccentColor;
            _titleText.fontStyle = FontStyle.Bold;

            var listRoot = new GameObject("Items", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listRoot.transform.SetParent(_contentRoot.transform, false);
            var listRect = (RectTransform)listRoot.transform;
            listRect.anchorMin = new Vector2(0.08f, 0.18f);
            listRect.anchorMax = new Vector2(0.92f, 0.82f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;
            var layout = listRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            listRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _itemsContainer = listRoot.transform;

            _emptyText = CreateText("EmptyMessage", _contentRoot.transform, 20, TextAnchor.MiddleCenter);
            var emptyRect = (RectTransform)_emptyText.transform;
            emptyRect.anchorMin = new Vector2(0.1f, 0.45f);
            emptyRect.anchorMax = new Vector2(0.9f, 0.62f);
            emptyRect.offsetMin = Vector2.zero;
            emptyRect.offsetMax = Vector2.zero;
            _emptyText.text = "No rewards assigned";
            _emptyText.color = Color.white;
            _emptyText.gameObject.SetActive(false);

            _continueButton = CreateButton("PreviewContinueButton", _contentRoot.transform, _continueLabel);
            var buttonRect = (RectTransform)_continueButton.transform;
            buttonRect.anchorMin = new Vector2(0.52f, 0.04f);
            buttonRect.anchorMax = new Vector2(0.88f, 0.14f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            _continueButton.onClick.AddListener(OnContinueClicked);

            _previousButton = CreateButton("PreviewPreviousButton", _contentRoot.transform, _previousLabel);
            var previousButtonRect = (RectTransform)_previousButton.transform;
            previousButtonRect.anchorMin = new Vector2(0.12f, 0.04f);
            previousButtonRect.anchorMax = new Vector2(0.48f, 0.14f);
            previousButtonRect.offsetMin = Vector2.zero;
            previousButtonRect.offsetMax = Vector2.zero;
            _previousButton.onClick.AddListener(OnPreviousClicked);
        }

        private void CreateItemRow(Data.ItemData item)
        {
            if (item == null)
                return;

            var row = new GameObject("ItemReward", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            row.transform.SetParent(_itemsContainer, false);
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 92f;
            rowLayout.preferredHeight = 92f;

            var imageObject = new GameObject("ItemImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(row.transform, false);
            var imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = new Vector2(0.02f, 0.08f);
            imageRect.anchorMax = new Vector2(0.18f, 0.92f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            var rawImage = imageObject.GetComponent<RawImage>();
            var sprite = _gameManager?.MediaDisplay?.ResolveItemSprite(item);
            rawImage.texture = sprite != null ? sprite.texture : null;
            rawImage.color = Color.white;

            var title = CreateText("ItemTitle", row.transform, 18, TextAnchor.UpperLeft);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0.21f, 0.48f);
            titleRect.anchorMax = new Vector2(0.97f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.text = string.IsNullOrEmpty(item.title) ? item.id : item.title;
            title.color = AccentColor;
            title.fontStyle = FontStyle.Bold;

            var description = CreateText("ItemDescription", row.transform, 14, TextAnchor.UpperLeft);
            var descriptionRect = (RectTransform)description.transform;
            descriptionRect.anchorMin = new Vector2(0.21f, 0.08f);
            descriptionRect.anchorMax = new Vector2(0.97f, 0.48f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;
            description.text = item.description ?? string.Empty;
            description.color = Color.white;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            _itemRows.Add(row);
        }

        private void ClearRows()
        {
            foreach (var row in _itemRows)
            {
                if (row != null)
                    Destroy(row);
            }
            _itemRows.Clear();
        }

        private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f, 1f);
            var text = CreateText("Text", go.transform, 18, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.05f, 0.1f, 0.06f);
            text.fontStyle = FontStyle.Bold;
            return go.GetComponent<Button>();
        }

        private void OnContinueClicked()
        {
            NextRequested?.Invoke();
        }

        private void OnPreviousClicked()
        {
            PreviousRequested?.Invoke();
        }
    }

}
