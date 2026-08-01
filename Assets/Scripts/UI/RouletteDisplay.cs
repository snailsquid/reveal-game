using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header("Winner Confirmation")]
        [Tooltip("Prefab-authored panel shown after each reward. The Next button must call ContinueAfterWinner.")]
        [SerializeField] private GameObject _winnerConfirmationScreen;
        [FormerlySerializedAs("_winnerConfirmationText")]
        [SerializeField] private TMP_Text _winnerConfirmationGameText;
        [SerializeField] private TMP_Text _winnerConfirmationDeveloperText;
        [SerializeField] private Image _winnerConfirmationImage;
        [SerializeField] private TMP_Text _winnerConfirmationNextLabel;

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
        private bool _winnerConfirmationAdvanceRequested;
        private UiTransition _rouletteTransition;
        private UiTransition _winnerConfirmationTransition;

        private void Awake()
        {
            _navigationStep = GetComponent<HintGameNavigationStep>();
            _rouletteTransition = UiTransition.GetOrAdd(_rouletteRoot);
            _winnerConfirmationTransition = UiTransition.GetOrAdd(_winnerConfirmationScreen);
            _winnerConfirmationTransition?.SetVisibleImmediately(false);
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
            _rouletteTransition ??= UiTransition.GetOrAdd(_rouletteRoot);
            _rouletteTransition?.Show();

            HideWinnerConfirmationImmediately();
            if (rewardItemIds == null)
            {
                CompleteDisplay();
                yield break;
            }

            for (int rewardIndex = 0; rewardIndex < rewardItemIds.Count; rewardIndex++)
            {
                bool isLastReward = rewardIndex == rewardItemIds.Count - 1;
                yield return StartCoroutine(SpinReelForReward(rewardItemIds[rewardIndex]));
                yield return StartCoroutine(WaitForWinnerConfirmation(rewardItemIds[rewardIndex], isLastReward));
            }

            CompleteDisplay();
        }

        public void ShowFailure(string message)
        {
            if (_statusText != null)
                _statusText.text = message ?? "Failed to open lootbox";

            CompleteDisplay();
        }

        /// <summary>
        /// Called by the prefab-authored confirmation panel's Next button.
        /// </summary>
        public void ContinueAfterWinner()
        {
            _winnerConfirmationAdvanceRequested = true;
        }

        public void Dispose()
        {
            Hide();
        }

        private void Hide(System.Action onComplete = null)
        {
            HideWinnerConfirmationImmediately();

            _rouletteTransition ??= UiTransition.GetOrAdd(_rouletteRoot);
            if (_rouletteTransition != null)
                _rouletteTransition.Hide(onComplete);
            else
                onComplete?.Invoke();
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

        }

        private IEnumerator WaitForWinnerConfirmation(string rewardItemId, bool isLastReward)
        {
            // Keep test scenes and legacy prefabs playable while requiring the button whenever
            // the confirmation panel is configured on the production prefab.
            if (_winnerConfirmationScreen == null)
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, _rewardInterval));
                yield break;
            }

            Data.ItemData reward = GetItemById(rewardItemId);
            string rewardTitle = reward != null ? reward.title : rewardItemId;
            if (_winnerConfirmationGameText != null)
                _winnerConfirmationGameText.text = rewardTitle ?? string.Empty;
            if (_winnerConfirmationDeveloperText != null)
                _winnerConfirmationDeveloperText.text = reward?.teamName ?? string.Empty;
            if (_winnerConfirmationImage != null)
            {
                _winnerConfirmationImage.sprite = ResolveItemSprite(reward);
                _winnerConfirmationImage.enabled = _winnerConfirmationImage.sprite != null;
            }
            if (_winnerConfirmationNextLabel != null)
                _winnerConfirmationNextLabel.text = isLastReward ? "Back to Lootboxes" : "Next Winner";

            _winnerConfirmationAdvanceRequested = false;
            _winnerConfirmationTransition ??= UiTransition.GetOrAdd(_winnerConfirmationScreen);
            _winnerConfirmationTransition.Show();

            yield return new WaitUntil(() => _winnerConfirmationAdvanceRequested);
            yield return StartCoroutine(HideWinnerConfirmation());
        }

        private IEnumerator HideWinnerConfirmation()
        {
            if (_winnerConfirmationTransition == null || !_winnerConfirmationScreen.activeSelf)
            {
                _winnerConfirmationAdvanceRequested = false;
                yield break;
            }

            bool completed = false;
            _winnerConfirmationTransition.Hide(() => completed = true);
            yield return new WaitUntil(() => completed);

            _winnerConfirmationAdvanceRequested = false;
        }

        private void HideWinnerConfirmationImmediately()
        {
            _winnerConfirmationTransition ??= UiTransition.GetOrAdd(_winnerConfirmationScreen);
            _winnerConfirmationTransition?.SetVisibleImmediately(false);

            _winnerConfirmationAdvanceRequested = false;
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
            var itemImage = cardObj.transform.Find("ImageContainer/Image")?.GetComponent<Image>();
            var gameText = cardObj.transform.Find("Game")?.GetComponent<TMP_Text>();
            var developerText = cardObj.transform.Find("Developer")?.GetComponent<TMP_Text>();

            if (itemImage != null)
            {
                itemImage.sprite = item != null ? ResolveItemSprite(item) : null;
                itemImage.enabled = itemImage.sprite != null;
            }

            if (gameText != null)
                gameText.text = item?.title ?? string.Empty;

            if (developerText != null)
                developerText.text = item?.teamName ?? string.Empty;

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
            Hide(() => _navigationStep?.RequestNext());
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

        private Sprite ResolveItemSprite(Data.ItemData item)
        {
            if (item == null)
                return null;

            return item.sprite ?? _gameManager?.MediaDisplay?.ResolveItemSprite(item);
        }
    }
}
