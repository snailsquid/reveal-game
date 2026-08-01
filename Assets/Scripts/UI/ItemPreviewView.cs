using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Displays imported reward data using the hierarchy authored in its prefab.
    /// </summary>
    public sealed class ItemPreviewView : MonoBehaviour, IItemPreviewView
    {
        [SerializeField] private string _title = "Rewards";

        [Header("Prefab References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _emptyText;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private ItemPreviewRow _itemRowPrefab;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _previousButton;

        [Header("List Animation")]
        [SerializeField, Min(0.01f)] private float _itemAnimationDuration = 0.22f;
        [SerializeField, Min(0f)] private float _itemAnimationStaggerDelay = 0.08f;

        private GameManager _gameManager;
        private HintGameNavigationStep _navigationStep;
        private readonly List<ItemPreviewRow> _itemRows = new();
        private Sequence _listAnimation;

        private void Awake()
        {
            _navigationStep = GetComponent<HintGameNavigationStep>();

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);
            if (_previousButton != null)
                _previousButton.onClick.AddListener(OnPreviousClicked);
        }

        private void OnDisable()
        {
            StopListAnimation();
        }

        public void Setup(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Show(IReadOnlyList<Data.ItemData> items)
        {
            ClearRows();

            _titleText.text = _title;
            bool hasItems = items != null && items.Count > 0;
            _emptyText.gameObject.SetActive(!hasItems);

            if (hasItems)
            {
                foreach (var item in items)
                {
                    if (item == null)
                        continue;

                    var row = Instantiate(_itemRowPrefab, _itemsContainer);
                    row.Bind(item, _gameManager?.MediaDisplay);
                    _itemRows.Add(row);
                }

                _listAnimation = UiStaggeredListAnimator.Play(
                    _itemsContainer,
                    _itemAnimationDuration,
                    _itemAnimationStaggerDelay);
            }
        }

        public void Clear()
        {
            ClearRows();
            _emptyText.gameObject.SetActive(false);
        }

        public void SetPreviousInteractable(bool interactable)
        {
            if (_previousButton != null)
                _previousButton.interactable = interactable;
        }

        private void ClearRows()
        {
            StopListAnimation();
            foreach (var row in _itemRows)
            {
                if (row != null)
                {
                    // Destroy is deferred until the end of the frame. Hide old rows now so
                    // they cannot consume stagger positions while replacements are created.
                    row.gameObject.SetActive(false);
                    Destroy(row.gameObject);
                }
            }

            _itemRows.Clear();
        }

        private void StopListAnimation()
        {
            if (_listAnimation == null)
                return;

            _listAnimation.Kill();
            _listAnimation = null;
        }

        private void OnContinueClicked() => _navigationStep?.RequestNext();

        private void OnPreviousClicked() => _navigationStep?.RequestPrevious();
    }
}
