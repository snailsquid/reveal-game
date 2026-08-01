using System;
using System.Collections.Generic;
using RevealGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Controls a single lootbox button in the selection grid.
    /// Supports unopened and opened states, including the opened lootbox tooltip.
    /// </summary>
    public class LootboxButtonController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _button;
        [SerializeField] private Text _indexLabel;
        [SerializeField] private GameObject _openedOverlay;
        [SerializeField] private GameObject _tooltip;
        [SerializeField] private Transform _winnerContainer;
        [SerializeField] private GameObject _winnerPrefab;

        private int _index;
        private Action<int> _onClick;
        private readonly List<GameObject> _winnerInstances = new List<GameObject>();
        private UiTransition _tooltipTransition;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            ButtonInteractionAnimation.EnsureAttached(_button);
            _tooltipTransition = UiTransition.GetOrAdd(_tooltip);
        }

        /// <summary>
        /// Configures this button for a lootbox at the given index.
        /// </summary>
        public void Setup(int index, bool isOpened, Action<int> onClick, string labelText)
        {
            _index = index;
            _onClick = onClick;

            if (_indexLabel != null)
            {
                _indexLabel.text = labelText;
                _indexLabel.color = new Color(0.08f, 0.16f, 0.25f);
            }
            if (_button != null) _button.interactable = !isOpened;

            if (_openedOverlay != null)
                _openedOverlay.SetActive(isOpened);

            ClearWinnerInstances();
            if (_tooltip != null)
                _tooltipTransition?.SetVisibleImmediately(false);

            // Remove old listeners and add new one
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Shows the opened lootbox tooltip and creates one winner card per reward.
        /// </summary>
        public void ShowWinners(IReadOnlyList<ItemData> winners)
        {
            if (_tooltip == null)
                return;

            ClearWinnerInstances();
            _tooltipTransition ??= UiTransition.GetOrAdd(_tooltip);
            _tooltipTransition.Show();

            if (_winnerContainer == null || _winnerPrefab == null || winners == null)
                return;

            foreach (var winner in winners)
            {
                var winnerInstance = Instantiate(_winnerPrefab, _winnerContainer);
                winnerInstance.name = $"Winner_{winner?.title ?? winner?.id ?? "Unknown"}";

                var image = winnerInstance.GetComponent<Image>();
                if (image != null && winner?.sprite != null)
                    image.sprite = winner.sprite;

                _winnerInstances.Add(winnerInstance);
            }
        }

        private void ClearWinnerInstances()
        {
            foreach (var winnerInstance in _winnerInstances)
            {
                if (winnerInstance != null)
                    Destroy(winnerInstance);
            }

            _winnerInstances.Clear();
        }

        private void OnButtonClicked()
        {
            _onClick?.Invoke(_index);
        }
    }
}
