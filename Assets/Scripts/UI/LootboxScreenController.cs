using System.Collections.Generic;
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

        private GameManager _gameManager;
        private List<LootboxButtonController> _buttonControllers = new List<LootboxButtonController>();

        private void Awake()
        {
            _gameManager = FindAnyObjectByType<GameManager>();

            if (_lootboxGridContainer == null)
            {
                var grid = transform.Find("LootboxGrid");
                if (grid != null) _lootboxGridContainer = grid;
            }

            if (_backButton == null)
            {
                var back = transform.Find("BackButton");
                if (back != null) _backButton = back.GetComponent<Button>();
            }

            if (_lootboxButtonPrefab == null)
            {
                var prefab = transform.Find("LootboxGrid/LootboxButtonRef");
                if (prefab != null) _lootboxButtonPrefab = prefab.gameObject;
            }

            if (_lootboxGridContainer == null)
            {
                Debug.LogError("[LootboxScreenController] LootboxGrid not found");
                enabled = false;
                return;
            }

            if (_lootboxButtonPrefab == null)
            {
                Debug.LogError("[LootboxScreenController] LootboxButtonPrefab not found");
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
            RefreshGrid();
            FindAnyObjectByType<ResponsiveUiLayout>()?.RefreshLayout();
        }

        public void RefreshGrid()
        {
            if (_gameManager?.Session?.Config == null)
                return;

            foreach (var ctrl in _buttonControllers)
            {
                if (ctrl != null && ctrl.gameObject != null)
                    Destroy(ctrl.gameObject);
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
                ctrl.Setup(i, isOpened, OnLootboxClicked);
                if (isOpened)
                {
                    var winners = _gameManager.LootboxEngine.GetAllLootboxStates()[i].winnerItemIds;
                    ctrl.ShowWinners(BuildWinnerText(winners));
                }
                _buttonControllers.Add(ctrl);
            }
        }

        private string BuildWinnerText(List<string> winnerItemIds)
        {
            if (winnerItemIds == null || winnerItemIds.Count == 0)
                return "Opened\nNo prize";

            var titles = new List<string>();
            foreach (var itemId in winnerItemIds)
            {
                var item = _gameManager.LootboxEngine.GetItemById(itemId);
                titles.Add(item == null ? itemId : item.title);
            }

            return string.Join("\n", titles);
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
