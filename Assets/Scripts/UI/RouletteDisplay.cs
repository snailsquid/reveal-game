using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

namespace RevealGame.UI
{
    public class RouletteDisplay : MonoBehaviour, IRouletteView
    {
        [Header("UI References")]
        [SerializeField] private GameObject _rouletteRoot;
        [SerializeField] private Transform _reelContainer;
        [SerializeField] private GameObject _itemCardPrefab;
        [SerializeField] private Text _statusText;
        [Tooltip("Fixed visual indicator above the reel. Its authored rotation must point upward.")]
        [SerializeField] private RectTransform _pointer;

        [Header("Winner Confirmation")]
        [Tooltip("Prefab-authored panel shown after each reward. The Next button must call ContinueAfterWinner.")]
        [SerializeField] private GameObject _winnerConfirmationScreen;
        [FormerlySerializedAs("_winnerConfirmationText")]
        [SerializeField] private TMP_Text _winnerConfirmationGameText;
        [SerializeField] private TMP_Text _winnerConfirmationDeveloperText;
        [SerializeField] private Image _winnerConfirmationImage;
        [SerializeField] private TMP_Text _winnerConfirmationNextLabel;
        [Tooltip("Shown only when the winning item has a video clip. Plays the clip in the winner image area.")]
        [SerializeField] private Button _winnerConfirmationVideoButton;
        [SerializeField] private RawImage _winnerConfirmationVideoImage;
        [SerializeField] private VideoPlayer _winnerConfirmationVideoPlayer;

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

        [Header("Pointer Tick")]
        [Tooltip("Positive values rotate left when the pointer graphic faces upward.")]
        [SerializeField] private float _pointerTickAngle = 15f;
        [Min(0f)]
        [Tooltip("Reel speed at or below which the pointer uses its slow flick duration.")]
        [SerializeField] private float _pointerSlowFlickSpeed = 250f;
        [Min(0f)]
        [Tooltip("Reel speed at or above which the pointer uses its fast flick duration.")]
        [SerializeField] private float _pointerFastFlickSpeed = 3500f;
        [Min(0f)]
        [Tooltip("Duration of the leftward flick at slow reel speeds.")]
        [SerializeField] private float _pointerSlowFlickDuration = 0.14f;
        [Min(0f)]
        [Tooltip("Duration of the leftward flick at fast reel speeds.")]
        [SerializeField] private float _pointerFastFlickDuration = 0.025f;
        [Min(0f)]
        [Tooltip("Fixed Back-eased return duration after every flick.")]
        [SerializeField] private float _pointerTickReturnDuration = 0.16f;

        private GameManager _gameManager;
        private HintGameNavigationStep _navigationStep;
        private IReadOnlyList<Data.ItemData> _testItems;
        private List<GameObject> _reelItems = new List<GameObject>();
        private float _currentPosition = 0f;
        private bool _winnerConfirmationAdvanceRequested;
        private UiTransition _rouletteTransition;
        private UiTransition _winnerConfirmationTransition;
        private RouletteConfettiBurst _confettiBurst;
        private Coroutine _pointerTickCoroutine;
        private Quaternion _pointerRestingRotation;
        private RenderTexture _winnerVideoTexture;

        private void Awake()
        {
            _navigationStep = GetComponent<HintGameNavigationStep>();
            _rouletteTransition = UiTransition.GetOrAdd(_rouletteRoot);
            _winnerConfirmationTransition = UiTransition.GetOrAdd(_winnerConfirmationScreen);
            _winnerConfirmationTransition?.SetVisibleImmediately(false);
            if (_winnerConfirmationVideoButton != null)
                _winnerConfirmationVideoButton.onClick.AddListener(ShowWinnerVideo);
            _confettiBurst = GetComponent<RouletteConfettiBurst>();
            CachePointerRestingRotation();
        }

        private void OnDisable()
        {
            ResetPointerTick();
            StopWinnerVideo();
        }

        private void OnDestroy()
        {
            if (_winnerConfirmationVideoButton != null)
                _winnerConfirmationVideoButton.onClick.RemoveListener(ShowWinnerVideo);

            ReleaseWinnerVideoTexture();
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

            var previouslyRevealedRewardIds = new HashSet<string>();
            for (int rewardIndex = 0; rewardIndex < rewardItemIds.Count; rewardIndex++)
            {
                string rewardItemId = rewardItemIds[rewardIndex];
                bool isLastReward = rewardIndex == rewardItemIds.Count - 1;
                yield return StartCoroutine(SpinReelForReward(rewardItemId, previouslyRevealedRewardIds));
                yield return StartCoroutine(WaitForWinnerConfirmation(rewardItemId, isLastReward));

                if (!string.IsNullOrWhiteSpace(rewardItemId))
                    previouslyRevealedRewardIds.Add(rewardItemId);
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
            StopWinnerVideo();
            _winnerConfirmationAdvanceRequested = true;
        }

        /// <summary>
        /// Called by the winner confirmation's View Video button.
        /// </summary>
        public void ShowWinnerVideo()
        {
            if (_winnerConfirmationVideoPlayer == null
                || _winnerConfirmationVideoPlayer.clip == null
                || _winnerConfirmationVideoImage == null)
                return;

            // The View Video button doubles as a toggle while the confirmation panel is open.
            // Keep the configured clip so the player can be started again without rebuilding
            // the winner state.
            if (_winnerConfirmationVideoImage.gameObject.activeSelf)
            {
                HideWinnerVideo();
                return;
            }

            EnsureWinnerVideoTexture(_winnerConfirmationVideoPlayer.clip);
            _winnerConfirmationImage?.gameObject.SetActive(false);
            _winnerConfirmationVideoImage.gameObject.SetActive(true);
            _winnerConfirmationVideoPlayer.Play();
        }

        public void Dispose()
        {
            Hide();
        }

        private void Hide(System.Action onComplete = null)
        {
            HideWinnerConfirmationImmediately();
            ResetPointerTick();

            _rouletteTransition ??= UiTransition.GetOrAdd(_rouletteRoot);
            if (_rouletteTransition != null)
                _rouletteTransition.Hide(onComplete);
            else
                onComplete?.Invoke();
        }

        private IEnumerator SpinReelForReward(
            string rewardItemId,
            HashSet<string> previouslyRevealedRewardIds)
        {
            float spinDuration = Mathf.Max(0f, _spinDuration);
            UiSoundPlayer.PlaySpin(spinDuration);
            ClearReel();
            BuildReel(rewardItemId, previouslyRevealedRewardIds);

            float targetPosition = _winningItemIndex * _itemSpacing - _winningItemTargetX;
            float startPosition = 0f;
            float elapsed = 0f;

            if (_statusText != null)
                _statusText.text = "Spinning...";

            while (elapsed < spinDuration)
            {
                float previousPosition = _currentPosition;
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / spinDuration);
                float progress = EvaluateSpinProgress(normalizedTime);

                _currentPosition = Mathf.LerpUnclamped(startPosition, targetPosition, progress);
                UpdateReelPosition();
                TriggerPointerTicksForCrossedBoundaries(previousPosition, _currentPosition);

                yield return null;
            }

            float positionBeforeFinalUpdate = _currentPosition;
            _currentPosition = targetPosition;
            UpdateReelPosition();
            TriggerPointerTicksForCrossedBoundaries(positionBeforeFinalUpdate, _currentPosition);

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, _revealPauseDuration));

            var reward = GetItemById(rewardItemId);
            string rewardTitle = reward != null ? reward.title : rewardItemId;
            if (_statusText != null)
                _statusText.text = $"Won: {rewardTitle}";

            _confettiBurst?.Play();

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
            ConfigureWinnerVideo(reward);
            if (_winnerConfirmationGameText != null)
                _winnerConfirmationGameText.text = rewardTitle ?? string.Empty;
            if (_winnerConfirmationDeveloperText != null)
                _winnerConfirmationDeveloperText.text = reward?.teamName ?? string.Empty;
            if (_winnerConfirmationImage != null)
            {
                _winnerConfirmationImage.sprite = ResolveItemSprite(reward);
                _winnerConfirmationImage.enabled = _winnerConfirmationImage.sprite != null;
                ItemImageAlignmentUtility.Apply(_winnerConfirmationImage, reward);
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
            StopWinnerVideo();
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
            StopWinnerVideo();
            _winnerConfirmationTransition ??= UiTransition.GetOrAdd(_winnerConfirmationScreen);
            _winnerConfirmationTransition?.SetVisibleImmediately(false);

            _winnerConfirmationAdvanceRequested = false;
        }

        private void ConfigureWinnerVideo(Data.ItemData reward)
        {
            StopWinnerVideo();

            VideoClip videoClip = reward?.videoClip;
            bool hasVideo = videoClip != null
                && _winnerConfirmationVideoButton != null
                && _winnerConfirmationVideoImage != null
                && _winnerConfirmationVideoPlayer != null;

            if (_winnerConfirmationVideoButton != null)
                _winnerConfirmationVideoButton.gameObject.SetActive(hasVideo);
            if (_winnerConfirmationVideoImage != null)
                _winnerConfirmationVideoImage.gameObject.SetActive(false);

            if (!hasVideo)
                return;

            _winnerConfirmationVideoPlayer.clip = videoClip;
            _winnerConfirmationVideoPlayer.isLooping = true;
            EnsureWinnerVideoTexture(videoClip);
        }

        private void StopWinnerVideo()
        {
            if (_winnerConfirmationVideoPlayer != null)
            {
                _winnerConfirmationVideoPlayer.Stop();
                _winnerConfirmationVideoPlayer.clip = null;
            }

            if (_winnerConfirmationVideoImage != null)
                _winnerConfirmationVideoImage.gameObject.SetActive(false);
            if (_winnerConfirmationImage != null)
                _winnerConfirmationImage.gameObject.SetActive(true);
        }

        private void HideWinnerVideo()
        {
            if (_winnerConfirmationVideoPlayer != null)
                _winnerConfirmationVideoPlayer.Stop();

            if (_winnerConfirmationVideoImage != null)
                _winnerConfirmationVideoImage.gameObject.SetActive(false);
            if (_winnerConfirmationImage != null)
                _winnerConfirmationImage.gameObject.SetActive(true);
        }

        private void EnsureWinnerVideoTexture(VideoClip videoClip)
        {
            if (_winnerConfirmationVideoPlayer == null || _winnerConfirmationVideoImage == null || videoClip == null)
                return;

            int width = (int)Mathf.Max(1, videoClip.width);
            int height = (int)Mathf.Max(1, videoClip.height);
            if (_winnerVideoTexture == null || _winnerVideoTexture.width != width || _winnerVideoTexture.height != height)
            {
                ReleaseWinnerVideoTexture();
                _winnerVideoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "WinnerVideoTexture"
                };
                _winnerVideoTexture.Create();
            }

            _winnerConfirmationVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _winnerConfirmationVideoPlayer.targetTexture = _winnerVideoTexture;
            _winnerConfirmationVideoImage.texture = _winnerVideoTexture;
        }

        private void ReleaseWinnerVideoTexture()
        {
            if (_winnerConfirmationVideoPlayer != null)
                _winnerConfirmationVideoPlayer.targetTexture = null;
            if (_winnerConfirmationVideoImage != null)
                _winnerConfirmationVideoImage.texture = null;

            if (_winnerVideoTexture == null)
                return;

            _winnerVideoTexture.Release();
            Destroy(_winnerVideoTexture);
            _winnerVideoTexture = null;
        }

        private float EvaluateSpinProgress(float normalizedTime)
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);

            // Existing prefabs that have not serialized the new curve retain the former SmoothStep motion.
            if (_spinEasing == null || _spinEasing.length == 0)
                return Mathf.SmoothStep(0f, 1f, clampedTime);

            return _spinEasing.Evaluate(clampedTime);
        }

        private void BuildReel(string winningItemId, HashSet<string> excludedItemIds)
        {
            List<Data.ItemData> allItems = GetReelItems()
                .Where(item => item == null
                    || string.IsNullOrWhiteSpace(item.id)
                    || excludedItemIds == null
                    || !excludedItemIds.Contains(item.id))
                .ToList();

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
                ItemImageAlignmentUtility.Apply(itemImage, item);
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

        /// <summary>
        /// Treats every boundary between two reel cards as an invisible pin. The reel only
        /// produces a tick while it moves left, so easing overshoot cannot generate reverse ticks.
        /// </summary>
        private void TriggerPointerTicksForCrossedBoundaries(float previousPosition, float currentPosition)
        {
            if (_pointer == null || _itemSpacing <= 0f || currentPosition <= previousPosition)
                return;

            float reelSpeed = (currentPosition - previousPosition) / Mathf.Max(Time.deltaTime, Mathf.Epsilon);
            for (int itemIndex = 0; itemIndex < _reelItems.Count - 1; itemIndex++)
            {
                float boundaryPosition = ((itemIndex + 0.5f) * _itemSpacing) - _winningItemTargetX;
                if (previousPosition < boundaryPosition && boundaryPosition <= currentPosition)
                    StartPointerTick(reelSpeed);
            }
        }

        private void StartPointerTick(float reelSpeed)
        {
            if (_pointer == null)
                return;

            UiSoundPlayer.PlayRouletteTick();

            if (_pointerTickCoroutine != null)
                StopCoroutine(_pointerTickCoroutine);

            _pointerTickCoroutine = StartCoroutine(PointerTickRoutine(GetPointerFlickDuration(reelSpeed)));
        }

        private IEnumerator PointerTickRoutine(float flickDuration)
        {
            Quaternion tickRotation = _pointerRestingRotation * Quaternion.Euler(0f, 0f, _pointerTickAngle);
            Quaternion startRotation = _pointer.localRotation;

            yield return RotatePointer(startRotation, tickRotation, flickDuration, false);
            yield return RotatePointer(tickRotation, _pointerRestingRotation, _pointerTickReturnDuration, true);

            _pointer.localRotation = _pointerRestingRotation;
            _pointerTickCoroutine = null;
        }

        private float GetPointerFlickDuration(float reelSpeed)
        {
            float slowSpeed = Mathf.Max(0f, _pointerSlowFlickSpeed);
            float fastSpeed = Mathf.Max(slowSpeed + Mathf.Epsilon, _pointerFastFlickSpeed);
            float speedProgress = Mathf.InverseLerp(slowSpeed, fastSpeed, reelSpeed);
            return Mathf.Lerp(_pointerSlowFlickDuration, _pointerFastFlickDuration, speedProgress);
        }

        private IEnumerator RotatePointer(Quaternion from, Quaternion to, float duration, bool useBackEasing)
        {
            if (_pointer == null)
                yield break;

            if (duration <= 0f)
            {
                _pointer.localRotation = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float progress = useBackEasing ? EaseOutBack(normalizedTime) : normalizedTime;
                _pointer.localRotation = Quaternion.SlerpUnclamped(from, to, progress);
                yield return null;
            }

            _pointer.localRotation = to;
        }

        private static float EaseOutBack(float normalizedTime)
        {
            const float overshoot = 1.70158f;
            float timeOffset = normalizedTime - 1f;
            return 1f + ((overshoot + 1f) * timeOffset * timeOffset * timeOffset)
                + (overshoot * timeOffset * timeOffset);
        }

        private void CachePointerRestingRotation()
        {
            if (_pointer != null)
                _pointerRestingRotation = _pointer.localRotation;
        }

        private void ResetPointerTick()
        {
            if (_pointerTickCoroutine != null)
            {
                StopCoroutine(_pointerTickCoroutine);
                _pointerTickCoroutine = null;
            }

            if (_pointer != null)
                _pointer.localRotation = _pointerRestingRotation;
        }

        private void ClearReel()
        {
            ResetPointerTick();

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
