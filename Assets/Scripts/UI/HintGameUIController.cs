using UnityEngine;
using UnityEngine.UI;
using System;

namespace RevealGame.UI
{
    public class HintGameUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _hintMediaImage;
        [SerializeField] private Text _hintTitleText;
        [SerializeField] private Button _previousHintImageButton;
        [SerializeField] private Button _nextHintImageButton;
        [SerializeField] private Text _hintImagePaginationText;
        [SerializeField] private InputField _answerInput;
        [SerializeField] private Button _submitButton;
        [SerializeField] private GameObject _resultPopup;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _discordStatusText;

        private GameManager _gameManager;
        private OpeningState _openingState;
        private int _currentHintImageIndex;
        private int _currentHintImageCount;
        private Coroutine _matchedAnswerRoutine;
        private Coroutine _discordStatusRefreshRoutine;

        private enum OpeningState
        {
            AwaitingStart,
            RevealingRewards,
            AwaitingExit
        }

        private void Awake()
        {
            _gameManager = FindAnyObjectByType<GameManager>();

            if (_hintMediaImage == null)
            {
                var m = transform.Find("HintMediaImage");
                if (m != null) _hintMediaImage = m.GetComponent<RawImage>();
            }
            if (_hintTitleText == null)
            {
                var t = transform.Find("HintTitleText");
                if (t != null) _hintTitleText = t.GetComponent<Text>();
            }
            if (_previousHintImageButton == null)
            {
                var previous = transform.Find("PreviousHintImageButton");
                if (previous != null) _previousHintImageButton = previous.GetComponent<Button>();
            }
            if (_nextHintImageButton == null)
            {
                var next = transform.Find("NextHintImageButton");
                if (next != null) _nextHintImageButton = next.GetComponent<Button>();
            }
            if (_hintImagePaginationText == null)
            {
                var pagination = transform.Find("HintImagePaginationText");
                if (pagination != null) _hintImagePaginationText = pagination.GetComponent<Text>();
            }
            if (_answerInput == null)
            {
                var i = transform.Find("AnswerInput");
                if (i != null) _answerInput = i.GetComponent<InputField>();
            }
            if (_submitButton == null)
            {
                var s = transform.Find("SubmitButton");
                if (s != null) _submitButton = s.GetComponent<Button>();
            }
            if (_resultPopup == null)
            {
                var p = transform.Find("ResultPopup");
                if (p != null) _resultPopup = p.gameObject;
            }
            if (_resultText == null && _resultPopup != null)
            {
                var r = _resultPopup.transform.Find("ResultText");
                if (r != null) _resultText = r.GetComponent<Text>();
            }
            if (_continueButton == null && _resultPopup != null)
            {
                var c = _resultPopup.transform.Find("ContinueButton");
                if (c != null) _continueButton = c.GetComponent<Button>();
            }
            if (_backButton == null)
            {
                var back = transform.Find("BackButton");
                if (back != null) _backButton = back.GetComponent<Button>();
            }

            if (_discordStatusText == null)
            {
                var status = transform.Find("DiscordStatusText");
                if (status != null) _discordStatusText = status.GetComponent<Text>();
            }

            if (_submitButton != null)
                SetButtonLabelColor(_submitButton, new Color(0.08f, 0.16f, 0.12f));

            EnsureHintImageCarouselControls();
            if (_previousHintImageButton != null)
                _previousHintImageButton.onClick.AddListener(ShowPreviousHintImage);
            if (_nextHintImageButton != null)
                _nextHintImageButton.onClick.AddListener(ShowNextHintImage);

            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinue);
                var label = _continueButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "Open Lootbox";
            }

            EnsureBackButton();
            EnsureDiscordStatusText();
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            if (_resultPopup != null)
                _resultPopup.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeToDiscordListener();
            _openingState = OpeningState.AwaitingStart;
            SetHintControlsVisible(true);
            SetContinueButtonLabel("Open Lootbox");

            if (_submitButton != null)
                _submitButton.interactable = true;

            if (_answerInput != null)
                _answerInput.text = string.Empty;

            if (_resultPopup != null)
                _resultPopup.SetActive(false);

            UpdateDiscordStatus();
            _discordStatusRefreshRoutine = StartCoroutine(RefreshDiscordStatus());
        }

        private void OnDisable()
        {
            if (_discordStatusRefreshRoutine != null)
            {
                StopCoroutine(_discordStatusRefreshRoutine);
                _discordStatusRefreshRoutine = null;
            }

            UnsubscribeFromDiscordListener();
            _gameManager?.DiscordAnswerListener?.StopListening();
        }

        public void ShowCurrentHint()
        {
            var hint = _gameManager.HintGameEngine.GetCurrentHint();
            if (hint == null)
            {
                _currentHintImageCount = 0;
                _currentHintImageIndex = 0;
                UpdateHintImageCarouselControls();
                return;
            }

            if (_hintTitleText != null)
                _hintTitleText.text = string.IsNullOrEmpty(hint.title) ? "Solve the puzzle!" : hint.title;

            _currentHintImageCount = hint.mediaPaths?.Length ?? 0;
            _currentHintImageIndex = 0;
            ShowHintImage(hint);
            UpdateHintImageCarouselControls();

            _gameManager.DiscordAnswerListener?.BeginListening(hint);
            UpdateDiscordStatus();
        }

        private void ShowPreviousHintImage()
        {
            if (_currentHintImageIndex == 0)
                return;

            _currentHintImageIndex--;
            RefreshCurrentHintImage();
        }

        private void ShowNextHintImage()
        {
            if (_currentHintImageIndex >= _currentHintImageCount - 1)
                return;

            _currentHintImageIndex++;
            RefreshCurrentHintImage();
        }

        private void RefreshCurrentHintImage()
        {
            var hint = _gameManager.HintGameEngine.GetCurrentHint();
            if (hint == null)
                return;

            ShowHintImage(hint);
            UpdateHintImageCarouselControls();
        }

        private void ShowHintImage(Data.HintData hint)
        {
            if (_hintMediaImage == null || _gameManager.MediaDisplay == null)
                return;

            if (_currentHintImageCount > 0)
            {
                string mediaPath = hint.mediaPaths[_currentHintImageIndex];
                if (!string.IsNullOrEmpty(mediaPath))
                {
                    var sprite = _gameManager.MediaDisplay.LoadSprite(mediaPath);
                    if (sprite != null)
                    {
                        _hintMediaImage.texture = sprite.texture;
                        _hintMediaImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        _hintMediaImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _hintMediaImage.gameObject.SetActive(false);
                }
            }
            else
            {
                _hintMediaImage.gameObject.SetActive(false);
            }
        }

        private void OnContinue()
        {
            if (_openingState == OpeningState.RevealingRewards)
                return;

            if (_openingState == OpeningState.AwaitingExit)
            {
                _gameManager.CompleteLootboxOpening();
                return;
            }

            _openingState = OpeningState.RevealingRewards;
            _continueButton.interactable = false;
            StartCoroutine(OpenLootboxRoutine());
        }

        private void OnBackClicked()
        {
            if (_openingState == OpeningState.RevealingRewards)
                return;

            if (_openingState == OpeningState.AwaitingExit)
            {
                _gameManager.CompleteLootboxOpening();
                return;
            }

            _gameManager.CancelLootboxOpening();
        }

        private void OnDiscordAnswerMatched(DiscordAnswerMatch match)
        {
            if (_openingState != OpeningState.AwaitingStart)
                return;

            var result = _gameManager.HintGameEngine.SubmitAnswer(match.Guess);
            if (!result.IsCorrect)
            {
                Debug.LogWarning("[HintGameUIController] Discord server returned an answer that does not match the active hint.");
                UpdateDiscordStatus();
                return;
            }

            SetHintControlsVisible(false);
            _gameManager.DiscordAnswerListener?.StopListening();
            if (_resultPopup != null && _resultText != null)
            {
                _resultText.text = $"Discord Answered Correctly\n{match.Username}\n'{match.Guess}'";
                ConfigureResultPopup();
                _resultPopup.SetActive(true);
            }

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(false);
            if (_backButton != null)
                _backButton.gameObject.SetActive(false);

            _matchedAnswerRoutine = StartCoroutine(ShowMatchThenOpenLootbox());
        }

        private System.Collections.IEnumerator ShowMatchThenOpenLootbox()
        {
            yield return new WaitForSecondsRealtime(3f);
            _matchedAnswerRoutine = null;
            OnContinue();
        }

        private System.Collections.IEnumerator OpenLootboxRoutine()
        {
            System.Collections.Generic.IReadOnlyList<string> rewardItemIds;
            try
            {
                rewardItemIds = _gameManager.OpenLootboxForReveal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowOpeningFailure();
                yield break;
            }

            var revealSequence = new LootboxRevealSequence(_gameManager, _resultText, _resultPopup.transform);
            yield return StartCoroutine(revealSequence.Play(rewardItemIds));

            revealSequence.Dispose();
            ShowRewardSummary(revealSequence.CollectedRewardTitles);
            _openingState = OpeningState.AwaitingExit;
            _continueButton.gameObject.SetActive(true);
            _continueButton.interactable = true;
            SetContinueButtonLabel("Back to Lootboxes");
        }

        private void ShowOpeningFailure()
        {
            if (_resultText != null)
                _resultText.text = "The lootbox could not be opened.";

            _openingState = OpeningState.AwaitingExit;
            _continueButton.interactable = true;
            SetContinueButtonLabel("Back to Lootboxes");
        }

        private void ShowRewardSummary(System.Collections.Generic.IReadOnlyList<string> collectedRewardTitles)
        {
            if (_resultText != null)
            {
                SetResultTextArea(0.25f);
                _resultText.text = collectedRewardTitles.Count == 0
                    ? "Lootbox opened\n\nNo prizes this time."
                    : $"Lootbox opened\n\nYou unlocked {collectedRewardTitles.Count} item(s):\n{string.Join("\n", collectedRewardTitles)}";
            }
        }

        private static void SetButtonLabelColor(Button button, Color color)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.color = color;
        }

        private void SetContinueButtonLabel(string labelText)
        {
            if (_continueButton == null)
                return;

            var label = _continueButton.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = labelText;
        }

        private void SetResultTextArea(float minY)
        {
            var textRect = _resultText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, minY);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void ConfigureResultPopup()
        {
            _resultText.fontSize = Screen.width < Screen.height ? 22 : 28;
            _resultText.color = Color.white;

            var popupImage = _resultPopup.GetComponent<Image>();
            if (popupImage != null) popupImage.color = new Color(0.05f, 0.10f, 0.18f, 0.96f);

            SetResultTextArea(0.66f);

            if (_continueButton == null)
                return;

            var buttonRect = _continueButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.18f, 0.04f);
            buttonRect.anchorMax = new Vector2(0.82f, 0.18f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
        }

        private void SetHintControlsVisible(bool visible)
        {
            if (_hintTitleText != null) _hintTitleText.gameObject.SetActive(visible);
            if (_hintMediaImage != null) _hintMediaImage.gameObject.SetActive(visible);
            SetHintImageCarouselControlsVisible(visible && _currentHintImageCount > 1);
            if (_answerInput != null) _answerInput.gameObject.SetActive(false);
            if (_submitButton != null) _submitButton.gameObject.SetActive(false);
            if (_backButton != null) _backButton.gameObject.SetActive(visible);
        }

        private void UpdateHintImageCarouselControls()
        {
            bool hasMultipleImages = _currentHintImageCount > 1;
            SetHintImageCarouselControlsVisible(hasMultipleImages);

            if (_previousHintImageButton != null)
                _previousHintImageButton.interactable = _currentHintImageIndex > 0;
            if (_nextHintImageButton != null)
                _nextHintImageButton.interactable = _currentHintImageIndex < _currentHintImageCount - 1;
            if (_hintImagePaginationText != null)
                _hintImagePaginationText.text = hasMultipleImages
                    ? $"{_currentHintImageIndex + 1} / {_currentHintImageCount}"
                    : string.Empty;
        }

        private void SetHintImageCarouselControlsVisible(bool visible)
        {
            if (_previousHintImageButton != null) _previousHintImageButton.gameObject.SetActive(visible);
            if (_nextHintImageButton != null) _nextHintImageButton.gameObject.SetActive(visible);
            if (_hintImagePaginationText != null) _hintImagePaginationText.gameObject.SetActive(visible);
        }

        private void EnsureHintImageCarouselControls()
        {
            if (_submitButton == null)
                return;

            if (_previousHintImageButton == null)
                _previousHintImageButton = CreateHintImageNavigationButton("PreviousHintImageButton", "<", new Vector2(0.30f, 0.70f));
            if (_nextHintImageButton == null)
                _nextHintImageButton = CreateHintImageNavigationButton("NextHintImageButton", ">", new Vector2(0.70f, 0.70f));

            if (_hintImagePaginationText != null)
                return;

            var paginationObject = new GameObject("HintImagePaginationText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            paginationObject.transform.SetParent(transform, false);
            var paginationRect = paginationObject.GetComponent<RectTransform>();
            paginationRect.anchorMin = new Vector2(0.42f, 0.52f);
            paginationRect.anchorMax = new Vector2(0.58f, 0.55f);
            paginationRect.offsetMin = Vector2.zero;
            paginationRect.offsetMax = Vector2.zero;

            _hintImagePaginationText = paginationObject.GetComponent<Text>();
            _hintImagePaginationText.font = _hintTitleText != null
                ? _hintTitleText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hintImagePaginationText.fontSize = 16;
            _hintImagePaginationText.color = Color.white;
            _hintImagePaginationText.alignment = TextAnchor.MiddleCenter;
        }

        private Button CreateHintImageNavigationButton(string name, string labelText, Vector2 anchor)
        {
            var buttonObject = Instantiate(_submitButton.gameObject, transform);
            buttonObject.name = name;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchor;
            buttonRect.anchorMax = anchor;
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(48f, 48f);

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = labelText;
                label.fontSize = 28;
            }

            return button;
        }

        private void EnsureBackButton()
        {
            if (_backButton != null || _submitButton == null)
                return;

            var backObject = Instantiate(_submitButton.gameObject, transform);
            backObject.name = "BackButton";
            _backButton = backObject.GetComponent<Button>();
            _backButton.onClick.RemoveAllListeners();

            var backRect = backObject.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.04f, 0.9f);
            backRect.anchorMax = new Vector2(0.23f, 0.97f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            var label = _backButton.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = "Back";
        }

        private void EnsureDiscordStatusText()
        {
            if (_discordStatusText != null)
                return;

            var statusObject = new GameObject("DiscordStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            statusObject.transform.SetParent(transform, false);
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.25f, 0.90f);
            statusRect.anchorMax = new Vector2(0.95f, 0.97f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            _discordStatusText = statusObject.GetComponent<Text>();
            _discordStatusText.font = _hintTitleText != null
                ? _hintTitleText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _discordStatusText.fontSize = Screen.width < Screen.height ? 14 : 16;
            _discordStatusText.alignment = TextAnchor.MiddleRight;
        }

        private void SubscribeToDiscordListener()
        {
            var listener = _gameManager?.DiscordAnswerListener;
            if (listener == null)
                return;

            listener.StatusChanged += UpdateDiscordStatus;
            listener.AnswerMatched += OnDiscordAnswerMatched;
        }

        private void UnsubscribeFromDiscordListener()
        {
            var listener = _gameManager?.DiscordAnswerListener;
            if (listener == null)
                return;

            listener.StatusChanged -= UpdateDiscordStatus;
            listener.AnswerMatched -= OnDiscordAnswerMatched;
        }

        private void UpdateDiscordStatus()
        {
            if (_discordStatusText == null)
                return;

            var listener = _gameManager?.DiscordAnswerListener;
            if (listener == null)
            {
                _discordStatusText.text = "Discord server unavailable.";
                _discordStatusText.color = new Color(1f, 0.54f, 0.54f);
                return;
            }

            _discordStatusText.text = listener.StatusMessage;
            _discordStatusText.color = listener.IsBackendConnected
                ? listener.IsListening ? new Color(0.22f, 0.85f, 0.59f) : Color.white
                : new Color(1f, 0.54f, 0.54f);
        }

        private System.Collections.IEnumerator RefreshDiscordStatus()
        {
            var delay = new WaitForSecondsRealtime(0.25f);
            while (true)
            {
                UpdateDiscordStatus();
                yield return delay;
            }
        }
    }
}
