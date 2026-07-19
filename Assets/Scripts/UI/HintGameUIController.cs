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
        [SerializeField] private InputField _answerInput;
        [SerializeField] private Button _submitButton;
        [SerializeField] private GameObject _resultPopup;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _backButton;

        private GameManager _gameManager;
        private OpeningState _openingState;

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

            if (_submitButton != null)
            {
                _submitButton.onClick.AddListener(OnSubmitAnswer);
                SetButtonLabelColor(_submitButton, new Color(0.08f, 0.16f, 0.12f));
            }

            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinue);
                var label = _continueButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = "Open Lootbox";
            }

            EnsureBackButton();
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            if (_resultPopup != null)
                _resultPopup.SetActive(false);
        }

        private void OnEnable()
        {
            _openingState = OpeningState.AwaitingStart;
            SetHintControlsVisible(true);
            SetContinueButtonLabel("Open Lootbox");

            if (_submitButton != null)
                _submitButton.interactable = true;

            if (_answerInput != null)
                _answerInput.text = string.Empty;

            if (_resultPopup != null)
                _resultPopup.SetActive(false);
        }

        public void ShowCurrentHint()
        {
            var hint = _gameManager.HintGameEngine.GetCurrentHint();
            if (hint == null)
                return;

            if (_hintTitleText != null)
                _hintTitleText.text = string.IsNullOrEmpty(hint.title) ? "Solve the puzzle!" : hint.title;

            if (_hintMediaImage != null && _gameManager.MediaDisplay != null)
            {
                if (hint.mediaPaths != null && hint.mediaPaths.Length > 0)
                {
                    string mediaPath = hint.mediaPaths[0];
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

            if (_answerInput != null)
                _answerInput.Select();
        }

        private void OnSubmitAnswer()
        {
            string answer = _answerInput != null ? _answerInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(answer))
                return;

            var result = _gameManager.HintGameEngine.SubmitAnswer(answer);

            if (result.IsCorrect)
            {
                if (_resultPopup != null && _resultText != null)
                {
                    _resultText.text = $"Player Answered Correctly\n'{result.Hint?.answer ?? answer}'";
                    ConfigureResultPopup();
                    SetHintControlsVisible(false);
                    _resultPopup.SetActive(true);
                }

                _submitButton.interactable = false;
            }
            else
            {
                if (_answerInput != null)
                {
                    _answerInput.text = string.Empty;
                    _answerInput.Select();
                }
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
            if (_answerInput != null) _answerInput.gameObject.SetActive(visible);
            if (_submitButton != null) _submitButton.gameObject.SetActive(visible);
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
    }
}
