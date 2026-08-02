using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Coordinates the work performed by each opening phase while leaving
    /// navigation itself to HintGameUIController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HintGameOpeningCoordinator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private HintGameUIController _navigation;
        [SerializeField] private HintGameView _hintView;

        private string _pendingGuesser;
        private string _pendingAnswer;
        private Sprite _pendingAnswerImage;
        private Coroutine _revealRoutine;
        private Coroutine _initialStepSyncRoutine;
        private bool _hintIsActive;
        private bool _isHandlingStep;
        private bool _discordSubscribed;
        private HintGameNavigationStep _lastHandledStep;

        private Data.GameSettings Settings => _gameManager?.Session?.Config?.settings;

        private bool HasConfiguredGame => _gameManager != null
            && _gameManager.Session != null
            && _gameManager.Session.Config != null;

        private void Awake()
        {
            if (_navigation == null)
                _navigation = GetComponent<HintGameUIController>();
            if (_gameManager == null)
                _gameManager = FindAnyObjectByType<GameManager>();

            _hintView ??= FindStepView<HintGameView>();
            _hintView?.Setup(_gameManager);
        }

        private void OnEnable()
        {
            if (_navigation == null)
            {
                Debug.LogError("[HintGameOpeningCoordinator] HintGameUIController is required.");
                return;
            }

            if (_navigation != null)
            {
                _navigation.StepChanged += OnStepChanged;
                _navigation.ExitRequested += OnExitRequested;
                _navigation.Finished += OnFinished;
            }

            if (_hintView != null)
                _hintView.AnswerSubmitted += OnHintAnswerSubmitted;

            SubscribeToDiscordListener();
            _lastHandledStep = null;
            _initialStepSyncRoutine = StartCoroutine(SynchronizeCurrentStepAfterNavigationReset());
        }

        private void OnDisable()
        {
            if (_initialStepSyncRoutine != null)
            {
                StopCoroutine(_initialStepSyncRoutine);
                _initialStepSyncRoutine = null;
            }

            _lastHandledStep = null;
            UnsubscribeFromDiscordListener();
            _gameManager?.DiscordAnswerListener?.StopListening();

            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            if (_navigation == null)
                return;

            if (_navigation != null)
            {
                _navigation.StepChanged -= OnStepChanged;
                _navigation.ExitRequested -= OnExitRequested;
                _navigation.Finished -= OnFinished;
            }

            if (_hintView != null)
                _hintView.AnswerSubmitted -= OnHintAnswerSubmitted;
        }

        private void OnStepChanged(HintGameNavigationStep step)
        {
            if (_isHandlingStep || step == null || step == _lastHandledStep)
                return;

            var previousStep = _lastHandledStep;
            _lastHandledStep = step;
            _isHandlingStep = true;
            try
            {
                if (!step.TryGetComponent<HintGameView>(out _) && _hintIsActive)
                {
                    _hintIsActive = false;
                    _gameManager?.DiscordAnswerListener?.StopListening();
                }

                if (step.TryGetComponent<ItemPreviewView>(out var previewView))
                {
                    // A correct answer begins a sting that transitions into the intermediate
                    // loop. Starting the default loop here would cancel that transition.
                    if (previousStep == null || !previousStep.TryGetComponent<AnswerPopupView>(out _))
                        UiSoundPlayer.PlayDefaultMusic();

                    ShowItemPreview(previewView);
                }
                else if (step.TryGetComponent<HintGameView>(out var hintView))
                {
                    UiSoundPlayer.PlayGuessingMusic();
                    BeginHintGame(hintView);
                }
                else if (step.TryGetComponent<AnswerPopupView>(out var answerPopupView))
                    ShowCorrectAnswerPopup(answerPopupView);
                else if (step.TryGetComponent<RouletteDisplay>(out var rouletteDisplay))
                    StartReveal(rouletteDisplay);
            }
            finally
            {
                _isHandlingStep = false;
            }
        }

        private IEnumerator SynchronizeCurrentStepAfterNavigationReset()
        {
            // The controller resets its current step from OnEnable. Waiting one frame
            // prevents processing the previous screen entry's final step, while also
            // covering the case where its StepChanged event ran before our subscription.
            yield return null;

            if (isActiveAndEnabled && _navigation?.CurrentStep != null)
                OnStepChanged(_navigation.CurrentStep);

            _initialStepSyncRoutine = null;
        }

        private void ShowItemPreview(ItemPreviewView previewView)
        {
            previewView.Setup(_gameManager);
            var contents = HasConfiguredGame
                ? _gameManager.LootboxEngine.GetCurrentContents()
                : new List<Data.ItemData>();
            previewView.Show(contents);
            previewView.SetPreviousInteractable(_navigation.CanPrevious);
        }

        private void BeginHintGame(HintGameView hintView)
        {
            if (!HasConfiguredGame)
            {
                Debug.LogWarning("[HintGameOpeningCoordinator] Cannot begin hint game until a game is configured.");
                return;
            }

            _gameManager.HintGameEngine.GetOrAssignHint();
            _hintIsActive = true;
            hintView.Setup(_gameManager);
            hintView.SetVisible(true);
            hintView.SetPreviousInteractable(_navigation.CanPrevious);
            hintView.ShowHint(_gameManager.HintGameEngine.GetCurrentHint());
        }

        private void ShowCorrectAnswerPopup(AnswerPopupView answerPopupView)
        {
            answerPopupView.Setup(_gameManager);
            answerPopupView.Show(
                _pendingGuesser ?? string.Empty,
                _pendingAnswer ?? string.Empty,
                _pendingAnswerImage,
                3f);
        }

        private void StartReveal(RouletteDisplay rouletteDisplay)
        {
            rouletteDisplay.Setup(_gameManager);
            if (_revealRoutine == null)
                _revealRoutine = StartCoroutine(OpenLootboxRoutine(rouletteDisplay));
        }

        private void OnHintAnswerSubmitted(string answer)
        {
            if (!_hintIsActive)
                return;

            ProcessAnswer(answer, "Player");
        }

        private void OnDiscordAnswerMatched(DiscordAnswerMatch match)
        {
            if (!_hintIsActive)
                return;

            ProcessAnswer(match.Guess, match.Username);
        }

        private void ProcessAnswer(string answer, string username)
        {
            var result = _gameManager.HintGameEngine.SubmitAnswer(answer);
            if (!result.IsCorrect)
            {
                Debug.LogWarning($"[{GetType().Name}] Answer does not match hint.");
                return;
            }

            _hintIsActive = false;
            _gameManager.DiscordAnswerListener?.StopListening();
            _pendingGuesser = username;
            _pendingAnswer = answer;
            _pendingAnswerImage = result.Hint?.answerImage;
            UiSoundPlayer.PlayGuessCorrectThenIntermediate();
            _hintView?.GetComponent<HintGameNavigationStep>()?.RequestNext();
        }

        private IEnumerator OpenLootboxRoutine(RouletteDisplay rouletteDisplay)
        {
            IReadOnlyList<string> rewardItemIds;
            try
            {
                rewardItemIds = _gameManager.OpenLootboxForReveal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                rouletteDisplay.ShowFailure(Settings?.FormatLootboxOpenFailed()
                    ?? Data.GameSettings.DefaultLootboxOpenFailedFormat);
                _revealRoutine = null;
                yield break;
            }

            yield return StartCoroutine(rouletteDisplay.Play(rewardItemIds));
            _revealRoutine = null;
        }

        private void OnExitRequested()
        {
            StopListeningAndReturnToLootboxes(cancelOpening: true);
        }

        private void OnFinished()
        {
            StopListeningAndReturnToLootboxes(cancelOpening: false);
        }

        private void StopListeningAndReturnToLootboxes(bool cancelOpening)
        {
            _gameManager?.DiscordAnswerListener?.StopListening();
            if (_gameManager == null)
                return;

            if (cancelOpening)
                _gameManager.CancelLootboxOpening();
            else
                _gameManager.CompleteLootboxOpening();
        }

        private T FindStepView<T>() where T : Component
            => GetComponentInChildren<T>(true)
                ?? FindAnyObjectByType<T>(FindObjectsInactive.Include);

        private void SubscribeToDiscordListener()
        {
            if (_discordSubscribed)
                return;

            var listener = _gameManager?.DiscordAnswerListener;
            if (listener != null)
            {
                listener.AnswerMatched += OnDiscordAnswerMatched;
                _discordSubscribed = true;
            }
        }

        private void UnsubscribeFromDiscordListener()
        {
            var listener = _gameManager?.DiscordAnswerListener;
            if (listener != null)
            {
                listener.AnswerMatched -= OnDiscordAnswerMatched;
                _discordSubscribed = false;
            }
        }
    }
}
