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

        [Header("Reel Settings")]
        [SerializeField] private int _totalItemsOnReel = 40;
        [SerializeField] private int _winningItemIndex = 15;
        [SerializeField] private float _itemSpacing = 120f;
        [Tooltip("The reel-space X position where the winning item's centre lands.")]
        [SerializeField] private float _winningItemTargetX = 50f;

        [Header("Spin Timing")]
        [Min(0f)]
        [SerializeField] private float _spinDuration = 3f;
        [Tooltip("Maps normalized spin time (X) to reel progress (Y). Values outside 0-1 allow intentional overshoot.")]
        [SerializeField] private AnimationCurve _spinEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Min(0f)]
        [SerializeField] private float _rewardInterval = 0.5f;
        [Min(0f)]
        [SerializeField] private float _revealPauseDuration = 0.3f;
        [Min(0f)]
        [SerializeField] private float _rewardDisplayDuration = 1f;

        private GameManager _gameManager;
        private HintGameNavigationStep _navigationStep;
        private IReadOnlyList<Data.ItemData> _testItems;
        private List<GameObject> _reelItems = new List<GameObject>();
        private float _currentPosition = 0f;

        private void Awake()
        {
            _navigationStep = GetComponent<HintGameNavigationStep>();
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
            if (_rouletteRoot != null)
                _rouletteRoot.SetActive(true);

            foreach (var itemId in rewardItemIds)
            {
                yield return StartCoroutine(SpinReelForReward(itemId));
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, _rewardInterval));
            }

            CompleteDisplay();
        }

        public void ShowFailure(string message)
        {
            if (_statusText != null)
                _statusText.text = message ?? "Failed to open lootbox";

            CompleteDisplay();
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

            float targetPosition = _winningItemIndex * _itemSpacing - _winningItemTargetX;
            float startPosition = 0f;
            float elapsed = 0f;
            float spinDuration = Mathf.Max(0f, _spinDuration);

            if (_statusText != null)
                _statusText.text = "Spinning...";

            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / spinDuration);
                float progress = EvaluateSpinProgress(normalizedTime);

                _currentPosition = Mathf.LerpUnclamped(startPosition, targetPosition, progress);
                UpdateReelPosition();

                yield return null;
            }

            _currentPosition = targetPosition;
            UpdateReelPosition();

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, _revealPauseDuration));

            var reward = GetItemById(rewardItemId);
            string rewardTitle = reward != null ? reward.title : rewardItemId;
            if (_statusText != null)
                _statusText.text = $"Won: {rewardTitle}";

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, _rewardDisplayDuration));
        }

        private float EvaluateSpinProgress(float normalizedTime)
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);

            // Existing prefabs that have not serialized the new curve retain the former SmoothStep motion.
            if (_spinEasing == null || _spinEasing.length == 0)
                return Mathf.SmoothStep(0f, 1f, clampedTime);

            return _spinEasing.Evaluate(clampedTime);
        }

        private void BuildReel(string winningItemId)
        {
            List<Data.ItemData> allItems = GetReelItems();

            if (allItems.Count == 0)
                return;

            System.Random rng = new System.Random();
            var reelItemIds = new List<string>(_totalItemsOnReel);
            for (int i = 0; i < _totalItemsOnReel; i++)
                reelItemIds.Add(null);

            if (_winningItemIndex >= 0 && _winningItemIndex < reelItemIds.Count)
            {
                reelItemIds[_winningItemIndex] = winningItemId;

                // Build outward from the fixed reward. This prevents either adjacent card from
                // revealing the reward and avoids duplicate neighbours across the whole reel.
                for (int i = _winningItemIndex - 1; i >= 0; i--)
                    reelItemIds[i] = SelectFillerItemId(allItems, null, reelItemIds[i + 1], rng);

                for (int i = _winningItemIndex + 1; i < reelItemIds.Count; i++)
                    reelItemIds[i] = SelectFillerItemId(allItems, reelItemIds[i - 1], null, rng);
            }
            else
            {
                for (int i = 0; i < reelItemIds.Count; i++)
                {
                    string previousItemId = i > 0 ? reelItemIds[i - 1] : null;
                    reelItemIds[i] = SelectFillerItemId(allItems, previousItemId, null, rng);
                }
            }

            for (int i = 0; i < reelItemIds.Count; i++)
            {
                string itemId = reelItemIds[i];
                CreateReelItem(i, itemId);
            }

            UpdateReelPosition();
        }

        private static string SelectFillerItemId(
            IReadOnlyList<Data.ItemData> allItems,
            string previousItemId,
            string nextReservedItemId,
            System.Random rng)
        {
            var candidates = new List<Data.ItemData>();
            foreach (var item in allItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id))
                    continue;

                if (item.id == previousItemId || item.id == nextReservedItemId)
                    continue;

                candidates.Add(item);
            }

            // A one-item lootbox cannot be shown without repeats. Fall back gracefully rather
            // than leaving a reel slot empty when no distinct filler image is available.
            if (candidates.Count == 0)
            {
                foreach (var item in allItems)
                {
                    if (item != null && !string.IsNullOrWhiteSpace(item.id))
                        candidates.Add(item);
                }
            }

            return candidates.Count > 0
                ? candidates[rng.Next(candidates.Count)].id
                : null;
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

        private void CompleteDisplay()
        {
            Hide();
            _navigationStep?.RequestNext();
        }

        private List<Data.ItemData> GetReelItems()
        {
            if (_gameManager?.LootboxEngine != null)
                return _gameManager.LootboxEngine.GetCurrentContents();

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
