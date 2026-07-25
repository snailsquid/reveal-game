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

        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;

        private OpeningState _openingState;
        private int _currentHintImageIndex;
        private int _currentHintImageCount;
        private Coroutine _matchedAnswerRoutine;

        private Data.GameSettings Settings => _gameManager?.Session?.Config?.settings;

        private enum OpeningState
        {
            AwaitingStart,
            RevealingRewards,
            AwaitingExit
        }

        private void Awake()
        {
            if (_submitButton != null)
                SetButtonLabelColor(_submitButton, new Color(0.08f, 0.16f, 0.12f));

            if (_previousHintImageButton != null)
                _previousHintImageButton.onClick.AddListener(ShowPreviousHintImage);
            if (_nextHintImageButton != null)
                _nextHintImageButton.onClick.AddListener(ShowNextHintImage);

            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinue);
                var label = _continueButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = Settings?.FormatOpenLootboxButtonLabel() ?? Data.GameSettings.DefaultOpenLootboxButtonLabelFormat;
            }

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
            SetContinueButtonLabel(Settings?.FormatOpenLootboxButtonLabel() ?? Data.GameSettings.DefaultOpenLootboxButtonLabelFormat);

            if (_submitButton != null)
                _submitButton.interactable = true;

            if (_answerInput != null)
                _answerInput.text = string.Empty;

            if (_resultPopup != null)
                _resultPopup.SetActive(false);
        }

        private void OnDisable()
        {
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
                _hintTitleText.text = string.IsNullOrEmpty(hint.title) ? (Settings?.FormatDefaultHintTitle() ?? Data.GameSettings.DefaultDefaultHintTitleFormat) : hint.title;

            _currentHintImageCount = _gameManager.MediaDisplay?.GetHintMediaCount(hint) ?? 0;
            _currentHintImageIndex = 0;
            ShowHintImage(hint);
            UpdateHintImageCarouselControls();

            _gameManager.DiscordAnswerListener?.BeginListening(hint);
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
                var sprite = _gameManager.MediaDisplay.ResolveHintSprite(hint, _currentHintImageIndex);
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
                return;
            }

            SetHintControlsVisible(false);
            _gameManager.DiscordAnswerListener?.StopListening();
            if (_resultPopup != null && _resultText != null)
            {
                _resultText.text = _gameManager.Session.Config.settings.FormatCorrectAnswer(match.Username, match.Guess);
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
            SetContinueButtonLabel(Settings?.FormatBackToLootboxesButtonLabel() ?? Data.GameSettings.DefaultBackToLootboxesButtonLabelFormat);
        }

        private void ShowOpeningFailure()
        {
            if (_resultText != null)
                _resultText.text = Settings?.FormatLootboxOpenFailed() ?? Data.GameSettings.DefaultLootboxOpenFailedFormat;

            _openingState = OpeningState.AwaitingExit;
            _continueButton.interactable = true;
            SetContinueButtonLabel(Settings?.FormatBackToLootboxesButtonLabel() ?? Data.GameSettings.DefaultBackToLootboxesButtonLabelFormat);
        }

        private void ShowRewardSummary(System.Collections.Generic.IReadOnlyList<string> collectedRewardTitles)
        {
            if (_resultText != null)
            {
                SetResultTextArea(0.25f);
                _resultText.text = collectedRewardTitles.Count == 0
                    ? (Settings?.FormatLootboxOpenedNoPrizes() ?? Data.GameSettings.DefaultLootboxOpenedNoPrizesFormat)
                    : (Settings?.FormatLootboxOpenedWithPrizes(collectedRewardTitles.Count, collectedRewardTitles) ?? Data.GameSettings.DefaultLootboxOpenedWithPrizesFormat);
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

        private void SubscribeToDiscordListener()
        {
            var listener = _gameManager?.DiscordAnswerListener;
            if (listener == null)
                return;

            listener.AnswerMatched += OnDiscordAnswerMatched;
        }

        private void UnsubscribeFromDiscordListener()
        {
            var listener = _gameManager?.DiscordAnswerListener;
            if (listener == null)
                return;

            listener.AnswerMatched -= OnDiscordAnswerMatched;
        }
    }
}
