using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    public class HintGameView : MonoBehaviour, IHintGameView
    {
        [Header("UI References")]
        [SerializeField] private RawImage _hintMediaImage;
        [SerializeField] private Text _hintTitleText;
        [SerializeField] private Button _previousHintImageButton;
        [SerializeField] private Button _nextHintImageButton;
        [SerializeField] private Text _hintImagePaginationText;
        [SerializeField] private InputField _answerInput;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _backButton;

        private GameManager _gameManager;
        private int _currentHintImageIndex;
        private int _currentHintImageCount;
        private Data.HintData _activeHint;
        private HintGameNavigationStep _navigationStep;

        private Data.GameSettings Settings
            => _gameManager?.Session?.Config?.settings;

        public event Action<string> AnswerSubmitted;

        private void Awake()
        {
            AutoDiscoverUIReferences();
            _navigationStep = GetComponent<HintGameNavigationStep>();

            if (_submitButton != null)
                SetButtonLabelColor(_submitButton, new Color(0.08f, 0.16f, 0.12f));

            if (_previousHintImageButton != null)
                _previousHintImageButton.onClick.AddListener(ShowPreviousHintImage);
            if (_nextHintImageButton != null)
                _nextHintImageButton.onClick.AddListener(ShowNextHintImage);

            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        private void AutoDiscoverUIReferences()
        {
            if (_hintMediaImage == null) _hintMediaImage = GetComponentInChildren<RawImage>(true);
            if (_hintTitleText == null) _hintTitleText = GetComponentInChildren<Text>(true);
            if (_previousHintImageButton == null)
                _previousHintImageButton = FindUIComponent<Button>("PreviousHintImageButton", "PreviousButton", "PrevButton");
            if (_nextHintImageButton == null)
                _nextHintImageButton = FindUIComponent<Button>("NextHintImageButton", "NextButton");
            if (_hintImagePaginationText == null)
                _hintImagePaginationText = FindUIComponent<Text>("HintImagePaginationText", "PaginationText");
            if (_backButton == null)
                _backButton = FindUIComponent<Button>("BackButton");
        }

        private T FindUIComponent<T>(params string[] names) where T : Component
        {
            var components = GetComponentsInChildren<T>(true);
            foreach (var name in names)
            {
                foreach (var comp in components)
                {
                    if (comp.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return comp;
                }
            }
            return null;
        }

        private void OnEnable()
        {
            if (_submitButton != null)
            {
                _submitButton.interactable = true;
                _submitButton.onClick.AddListener(OnSubmitClicked);
            }
        }

        private void OnDisable()
        {
            if (_submitButton != null)
                _submitButton.onClick.RemoveListener(OnSubmitClicked);
        }

        public void Setup(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void ShowHint(Data.HintData hint)
        {
            _activeHint = hint;

            if (hint == null)
            {
                _currentHintImageCount = 0;
                _currentHintImageIndex = 0;
                UpdateHintImageCarouselControls();
                return;
            }

            if (_hintTitleText != null)
                _hintTitleText.text = string.IsNullOrEmpty(hint.title)
                    ? (Settings?.FormatDefaultHintTitle() ?? Data.GameSettings.DefaultDefaultHintTitleFormat)
                    : hint.title;

            _currentHintImageCount = _gameManager.MediaDisplay?.GetHintMediaCount(hint) ?? 0;
            _currentHintImageIndex = 0;
            ShowHintImage(hint);
            UpdateHintImageCarouselControls();

            _gameManager.DiscordAnswerListener?.BeginListening(hint);
        }

        public void Clear()
        {
            _activeHint = null;
            _currentHintImageCount = 0;
            _currentHintImageIndex = 0;

            if (_hintMediaImage != null)
            {
                _hintMediaImage.texture = null;
                _hintMediaImage.gameObject.SetActive(false);
            }

            if (_hintTitleText != null)
                _hintTitleText.text = string.Empty;

            if (_answerInput != null)
                _answerInput.text = string.Empty;

            UpdateHintImageCarouselControls();
        }

        public void SetVisible(bool visible)
        {
            if (_hintTitleText != null) _hintTitleText.gameObject.SetActive(visible);
            if (_hintMediaImage != null) _hintMediaImage.gameObject.SetActive(visible);
            SetHintImageCarouselControlsVisible(visible && _currentHintImageCount > 1);
            if (_answerInput != null) _answerInput.gameObject.SetActive(visible);
            if (_submitButton != null) _submitButton.gameObject.SetActive(visible);
            if (_backButton != null) _backButton.gameObject.SetActive(visible);
        }

        public void SetInteractable(bool interactable)
        {
            if (_previousHintImageButton != null) _previousHintImageButton.interactable = interactable;
            if (_nextHintImageButton != null) _nextHintImageButton.interactable = interactable;
            if (_submitButton != null) _submitButton.interactable = interactable;
        }

        public void SetPreviousInteractable(bool interactable)
        {
            if (_backButton != null)
                _backButton.interactable = interactable;
        }

        private void OnSubmitClicked()
        {
            if (_answerInput == null || string.IsNullOrWhiteSpace(_answerInput.text))
                return;

            var answer = _answerInput.text.Trim();
            _answerInput.text = string.Empty;
            AnswerSubmitted?.Invoke(answer);
        }

        private void OnBackClicked() => _navigationStep?.RequestPrevious();

        private void ShowPreviousHintImage()
        {
            if (_currentHintImageIndex == 0) return;
            _currentHintImageIndex--;
            RefreshCurrentHintImage();
        }

        private void ShowNextHintImage()
        {
            if (_currentHintImageIndex >= _currentHintImageCount - 1) return;
            _currentHintImageIndex++;
            RefreshCurrentHintImage();
        }

        private void RefreshCurrentHintImage()
        {
            if (_activeHint == null) return;
            ShowHintImage(_activeHint);
            UpdateHintImageCarouselControls();
        }

        private void ShowHintImage(Data.HintData hint)
        {
            if (_hintMediaImage == null || _gameManager == null)
            {
                Debug.LogWarning("[HintGameView] HintMediaImage or GameManager is null");
                return;
            }

            if (_gameManager.MediaDisplay == null)
            {
                Debug.LogWarning("[HintGameView] MediaDisplay is null");
                return;
            }

            if (_currentHintImageCount > 0)
            {
                var sprite = _gameManager.MediaDisplay.ResolveHintSprite(hint, _currentHintImageIndex);
                if (sprite != null && sprite.texture != null)
                {
                    _hintMediaImage.texture = sprite.texture;
                    _hintMediaImage.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"[HintGameView] Sprite or texture is null for hint at index {_currentHintImageIndex}");
                    _hintMediaImage.texture = null;
                    _hintMediaImage.gameObject.SetActive(false);
                }
            }
            else
            {
                _hintMediaImage.gameObject.SetActive(false);
            }
        }

        private void UpdateHintImageCarouselControls()
        {
            bool hasMultiple = _currentHintImageCount > 1;
            SetHintImageCarouselControlsVisible(hasMultiple);

            if (_previousHintImageButton != null)
                _previousHintImageButton.interactable = _currentHintImageIndex > 0;
            if (_nextHintImageButton != null)
                _nextHintImageButton.interactable = _currentHintImageIndex < _currentHintImageCount - 1;
            if (_hintImagePaginationText != null)
                _hintImagePaginationText.text = hasMultiple
                    ? $"{_currentHintImageIndex + 1} / {_currentHintImageCount}"
                    : string.Empty;
        }

        private void SetHintImageCarouselControlsVisible(bool visible)
        {
            if (_previousHintImageButton != null) _previousHintImageButton.gameObject.SetActive(visible);
            if (_nextHintImageButton != null) _nextHintImageButton.gameObject.SetActive(visible);
            if (_hintImagePaginationText != null) _hintImagePaginationText.gameObject.SetActive(visible);
        }

        private static void SetButtonLabelColor(Button button, Color color)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.color = color;
        }
    }
}
