using System.Collections.Generic;
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

        private GameManager _gameManager;
        private HintGameNavigationStep _navigationStep;
        private readonly List<ItemPreviewRow> _itemRows = new();

        private void Awake()
        {
            _navigationStep = GetComponent<HintGameNavigationStep>();

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);
            if (_previousButton != null)
                _previousButton.onClick.AddListener(OnPreviousClicked);
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
            foreach (var row in _itemRows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            _itemRows.Clear();
        }

        private void OnContinueClicked() => _navigationStep?.RequestNext();

        private void OnPreviousClicked() => _navigationStep?.RequestPrevious();
    }
}
