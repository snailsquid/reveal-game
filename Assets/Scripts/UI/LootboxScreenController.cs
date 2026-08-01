using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    public class LootboxScreenController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform _lootboxGridContainer;
        [SerializeField] private GameObject _lootboxButtonPrefab;
        [SerializeField] private Button _backButton;

        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private ResponsiveUiLayout _responsiveUiLayout;

        [Header("List Animation")]
        [SerializeField, Min(0.01f)] private float _lootboxAnimationDuration = 0.22f;
        [SerializeField, Min(0f)] private float _lootboxAnimationStaggerDelay = 0.08f;

        private List<LootboxButtonController> _buttonControllers = new List<LootboxButtonController>();
        private Sequence _listAnimation;

        private void Awake()
        {
            if (_lootboxGridContainer == null)
            {
                Debug.LogError("[LootboxScreenController] LootboxGrid not assigned");
                enabled = false;
                return;
            }

            if (_lootboxButtonPrefab == null)
            {
                Debug.LogError("[LootboxScreenController] LootboxButtonPrefab not assigned");
                enabled = false;
                return;
            }

            _lootboxButtonPrefab.SetActive(false);

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
                var label = _backButton.GetComponentInChildren<Text>(true);
                if (label != null) label.color = new Color(0.12f, 0.12f, 0.12f);
            }
        }

        private void OnEnable()
        {
            _responsiveUiLayout?.RefreshLayout();
        }

        private void OnDisable()
        {
            StopListAnimation();
        }

        public void RefreshGrid()
        {
            if (_gameManager?.Session?.Config == null)
                return;

            StopListAnimation();
            foreach (var ctrl in _buttonControllers)
            {
                if (ctrl != null && ctrl.gameObject != null)
                {
                    // Destroy is deferred until the end of the frame. Hide old buttons now so
                    // they cannot consume stagger positions while the replacement grid is built.
                    ctrl.gameObject.SetActive(false);
                    Destroy(ctrl.gameObject);
                }
            }
            _buttonControllers.Clear();

            int count = _gameManager.LootboxEngine.GetLootboxCount();
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(_lootboxButtonPrefab, _lootboxGridContainer);
                go.name = $"Lootbox_{i}";
                go.SetActive(true);

                var ctrl = go.GetComponent<LootboxButtonController>();
                if (ctrl == null)
                    ctrl = go.AddComponent<LootboxButtonController>();

                bool isOpened = _gameManager.LootboxEngine.IsLootboxOpened(i);
                var settings = _gameManager.Session?.Config?.settings;
                var labelText = settings != null
                    ? settings.FormatLootboxLabel(i + 1)
                    : Data.GameSettings.DefaultLootboxLabelFormat;
                ctrl.Setup(i, isOpened, OnLootboxClicked, labelText);
                if (isOpened)
                    ctrl.ShowWinners(GetWinnerItems(i));
                _buttonControllers.Add(ctrl);
            }

            _listAnimation = UiStaggeredListAnimator.Play(
                _lootboxGridContainer,
                _lootboxAnimationDuration,
                _lootboxAnimationStaggerDelay);
        }

        private void StopListAnimation()
        {
            if (_listAnimation == null)
                return;

            _listAnimation.Kill();
            _listAnimation = null;
        }

        private List<Data.ItemData> GetWinnerItems(int lootboxIndex)
        {
            var winnerItemIds = _gameManager.LootboxEngine.GetAllLootboxStates()[lootboxIndex].winnerItemIds;
            var winners = new List<Data.ItemData>();
            if (winnerItemIds == null)
                return winners;

            foreach (var itemId in winnerItemIds)
            {
                var item = _gameManager.LootboxEngine.GetItemById(itemId);
                if (item != null)
                    winners.Add(item);
            }

            return winners;
        }

        private void OnLootboxClicked(int index)
        {
            try
            {
                _gameManager.LootboxEngine.SelectLootbox(index);
                _gameManager.ShowHintGameScreen();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LootboxScreenController] Cannot select lootbox {index}: {e.Message}");
            }
        }

        private void OnBackClicked()
        {
            _gameManager.ShowTitleScreen();
        }
    }
}
