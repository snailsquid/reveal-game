using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    public class RouletteDisplay : MonoBehaviour, IRouletteView
    {
        [Header("UI References")]
        [SerializeField] private GameObject _rouletteRoot;
        [SerializeField] private Transform _reelContainer;
        [SerializeField] private GameObject _itemCardPrefab;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _continueButton;

        [Header("Reel Settings")]
        [SerializeField] private int _totalItemsOnReel = 40;
        [SerializeField] private int _winningItemIndex = 15;
        [SerializeField] private float _spinDuration = 3f;
        [SerializeField] private float _itemSpacing = 120f;

        private GameManager _gameManager;
        private IReadOnlyList<Data.ItemData> _testItems;
        private List<string> _rewardTitles = new List<string>();
        private List<GameObject> _reelItems = new List<GameObject>();
        private float _currentPosition = 0f;

        private Data.GameSettings Settings
            => _gameManager?.Session?.Config?.settings;

        public IReadOnlyList<string> CollectedRewardTitles => _rewardTitles;
        public event Action ContinueRequested;

        private void Awake()
        {
            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);
        }

        public void Setup(GameManager gameManager)
        {
            _gameManager = gameManager;
            _testItems = null;
        }

        /// <summary>
        /// Supplies item data for an isolated roulette display test scene.
        /// Production gameplay should use <see cref="Setup(GameManager)"/>.
        /// </summary>
        public void SetupTestItems(IReadOnlyList<Data.ItemData> items)
        {
            _gameManager = null;
            _testItems = items;
        }

        public IEnumerator Play(IReadOnlyList<string> rewardItemIds)
        {
            _rewardTitles.Clear();

            if (_rouletteRoot != null)
                _rouletteRoot.SetActive(true);

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);

            foreach (var itemId in rewardItemIds)
            {
                yield return StartCoroutine(SpinReelForReward(itemId));
                yield return new WaitForSecondsRealtime(0.5f);
            }

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(true);
        }

        public void ShowSummary(IReadOnlyList<string> rewardTitles)
        {
            if (_reelContainer != null)
            {
                foreach (Transform child in _reelContainer)
                    Destroy(child.gameObject);
                _reelItems.Clear();
            }

            if (_statusText != null && rewardTitles.Count > 0)
            {
                string summary = string.Join("\n", rewardTitles);
                _statusText.text = $"Rewards:\n{summary}";
            }
        }

        public void ShowFailure(string message)
        {
            if (_statusText != null)
                _statusText.text = message ?? "Failed to open lootbox";

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(true);
        }

        public void Dispose()
        {
            Hide();
        }

        private void Hide()
        {
            if (_rouletteRoot != null)
                _rouletteRoot.SetActive(false);
        }

        private IEnumerator SpinReelForReward(string rewardItemId)
        {
            ClearReel();
            BuildReel(rewardItemId);

            float targetPosition = _winningItemIndex * _itemSpacing - 50f;
            float startPosition = 0f;
            float elapsed = 0f;

            if (_statusText != null)
                _statusText.text = "Spinning...";

            while (elapsed < _spinDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _spinDuration);

                _currentPosition = Mathf.Lerp(startPosition, targetPosition, t);
                UpdateReelPosition();

                yield return null;
            }

            _currentPosition = targetPosition;
            UpdateReelPosition();

            yield return new WaitForSecondsRealtime(0.3f);

            var reward = GetItemById(rewardItemId);
            string rewardTitle = reward != null ? reward.title : rewardItemId;
            _rewardTitles.Add(rewardTitle);

            if (_statusText != null)
                _statusText.text = $"Won: {rewardTitle}";

            yield return new WaitForSecondsRealtime(1f);
        }

        private void BuildReel(string winningItemId)
        {
            List<Data.ItemData> allItems = GetAllItems();

            if (allItems.Count == 0)
                return;

            System.Random rng = new System.Random();

            for (int i = 0; i < _totalItemsOnReel; i++)
            {
                string itemId;

                if (i == _winningItemIndex)
                {
                    itemId = winningItemId;
                }
                else
                {
                    var randomItem = allItems[rng.Next(allItems.Count)];
                    itemId = randomItem.id;
                }

                CreateReelItem(i, itemId);
            }

            UpdateReelPosition();
        }

        private void CreateReelItem(int index, string itemId)
        {
            if (_itemCardPrefab == null || _reelContainer == null)
                return;

            GameObject cardObj = Instantiate(_itemCardPrefab, _reelContainer);
            cardObj.name = $"ReelItem_{index}";

            var item = GetItemById(itemId);
            var rawImage = cardObj.GetComponent<RawImage>();

            if (rawImage != null && item != null)
            {
                var sprite = item.sprite;
                var mediaDisplay = _gameManager?.MediaDisplay;
                if (sprite == null && mediaDisplay != null)
                    sprite = mediaDisplay.ResolveItemSprite(item);

                if (sprite != null && sprite.texture != null)
                {
                    RenderTexture renderTexture = new RenderTexture(
                        sprite.texture.width,
                        sprite.texture.height,
                        0,
                        RenderTextureFormat.ARGB32
                    );
                    Graphics.Blit(sprite.texture, renderTexture);
                    rawImage.texture = renderTexture;
                }
            }

            _reelItems.Add(cardObj);
        }

        private void UpdateReelPosition()
        {
            for (int i = 0; i < _reelItems.Count; i++)
            {
                var item = _reelItems[i];
                var rectTransform = item.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    float xPosition = (i * _itemSpacing) - _currentPosition;
                    rectTransform.anchoredPosition = new Vector2(xPosition, 0f);
                }
            }
        }

        private void ClearReel()
        {
            foreach (var item in _reelItems)
            {
                if (item != null)
                    Destroy(item);
            }
            _reelItems.Clear();
            _currentPosition = 0f;
        }

        private void OnContinueClicked()
        {
            Hide();
            ContinueRequested?.Invoke();
        }

        private List<Data.ItemData> GetAllItems()
        {
            if (_gameManager?.LootboxEngine != null)
                return _gameManager.LootboxEngine.GetAllItems();

            return _testItems != null
                ? new List<Data.ItemData>(_testItems)
                : new List<Data.ItemData>();
        }

        private Data.ItemData GetItemById(string itemId)
        {
            if (_gameManager?.LootboxEngine != null)
                return _gameManager.LootboxEngine.GetItemById(itemId);

            if (_testItems == null)
                return null;

            foreach (var item in _testItems)
            {
                if (item != null && item.id == itemId)
                    return item;
            }

            return null;
        }
    }
}
