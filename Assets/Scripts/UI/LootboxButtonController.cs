using System;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Controls a single lootbox button in the selection grid.
    /// Supports opened (greyed/disabled) and unopened states.
    /// </summary>
    public class LootboxButtonController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _button;
        [SerializeField] private Text _indexLabel;
        [SerializeField] private Text _winnerLabel;
        [SerializeField] private GameObject _openedOverlay;

        private int _index;
        private Action<int> _onClick;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
        }

        /// <summary>
        /// Configures this button for a lootbox at the given index.
        /// </summary>
        public void Setup(int index, bool isOpened, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            if (_indexLabel != null)
            {
                _indexLabel.text = $"Box {index + 1}";
                _indexLabel.color = new Color(0.08f, 0.16f, 0.25f);
            }
            if (_button != null) _button.interactable = !isOpened;

            if (_openedOverlay != null)
                _openedOverlay.SetActive(isOpened);

            if (_winnerLabel != null)
                _winnerLabel.gameObject.SetActive(false);

            // Remove old listeners and add new one
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Shows winner text above the lootbox after it's opened.
        /// </summary>
        public void ShowWinners(string winnerText)
        {
            if (_winnerLabel != null)
            {
                _winnerLabel.text = winnerText;
                _winnerLabel.gameObject.SetActive(true);
            }
        }

        private void OnButtonClicked()
        {
            _onClick?.Invoke(_index);
        }
    }
}
