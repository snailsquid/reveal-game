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
        [SerializeField] private PreGamePreviewView _preGamePreviewView;
        [SerializeField] private HintGameView _hintView;
        [SerializeField] private AnswerPopupView _answerPopupView;
        [SerializeField] private RouletteDisplay _rouletteDisplay;
        [SerializeField] private PostGamePreviewView _postGamePreviewView;

        private string _pendingAnswerMessage;
        private Coroutine _revealRoutine;
        private bool _hintIsActive;
        private bool _isHandlingStep;
        private bool _discordSubscribed;

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

            _preGamePreviewView ??= FindPhaseView<PreGamePreviewView>();
            _hintView ??= FindPhaseView<HintGameView>();
            _answerPopupView ??= FindPhaseView<AnswerPopupView>();
            _rouletteDisplay ??= FindPhaseView<RouletteDisplay>();
            _postGamePreviewView ??= FindPhaseView<PostGamePreviewView>();

            _preGamePreviewView?.Setup(_gameManager);
            _hintView?.Setup(_gameManager);
            _answerPopupView?.Setup(_gameManager);
            _rouletteDisplay?.Setup(_gameManager);
            _postGamePreviewView?.Setup(_gameManager);
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

            if (_preGamePreviewView != null)
            {
                _preGamePreviewView.NextRequested += _navigation.Next;
                _preGamePreviewView.PreviousRequested += _navigation.Previous;
            }

            if (_hintView != null)
            {
                _hintView.AnswerSubmitted += OnHintAnswerSubmitted;
                _hintView.BackRequested += _navigation.Previous;
            }

            if (_answerPopupView != null)
                _answerPopupView.Dismissed += _navigation.Next;

            if (_rouletteDisplay != null)
                _rouletteDisplay.ContinueRequested += _navigation.Next;

            if (_postGamePreviewView != null)
                _postGamePreviewView.NextRequested += _navigation.Next;

            SubscribeToDiscordListener();
        }

        private void Start()
        {
            SubscribeToDiscordListener();

            // The navigation component may have entered its first step before
            // this component subscribed, so explicitly process the current step.
            if (_navigation?.CurrentStep != null)
                OnStepChanged(_navigation.CurrentStep);
        }

        private void OnDisable()
        {
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

            if (_preGamePreviewView != null)
            {
                _preGamePreviewView.NextRequested -= _navigation.Next;
                _preGamePreviewView.PreviousRequested -= _navigation.Previous;
            }

            if (_hintView != null)
            {
                _hintView.AnswerSubmitted -= OnHintAnswerSubmitted;
                _hintView.BackRequested -= _navigation.Previous;
            }

            if (_answerPopupView != null)
                _answerPopupView.Dismissed -= _navigation.Next;

            if (_rouletteDisplay != null)
                _rouletteDisplay.ContinueRequested -= _navigation.Next;

            if (_postGamePreviewView != null)
                _postGamePreviewView.NextRequested -= _navigation.Next;
        }

        private void OnStepChanged(HintGameNavigationStep step)
        {
            if (_isHandlingStep || step == null)
                return;

            _isHandlingStep = true;
            try
            {
                HideInactiveViews(step);

                if (IsStepFor(step, _preGamePreviewView))
                {
                    ShowPreGamePreview();
                }
                else if (IsStepFor(step, _hintView))
                {
                    BeginHintGame();
                }
                else if (IsStepFor(step, _answerPopupView))
                {
                    ShowCorrectAnswerPopup();
                }
                else if (IsStepFor(step, _rouletteDisplay))
                {
                    StartReveal();
                }
                else if (IsStepFor(step, _postGamePreviewView))
                {
                    ShowPostGamePreview();
                }
            }
            finally
            {
                _isHandlingStep = false;
            }
        }

        private void HideInactiveViews(HintGameNavigationStep activeStep)
        {
            if (!IsStepFor(activeStep, _hintView) && _hintIsActive)
            {
                _hintIsActive = false;
                _gameManager?.DiscordAnswerListener?.StopListening();
            }

            if (!IsStepFor(activeStep, _preGamePreviewView))
                _preGamePreviewView?.SetVisible(false);
            if (!IsStepFor(activeStep, _hintView))
                _hintView?.SetVisible(false);
            if (!IsStepFor(activeStep, _answerPopupView))
                _answerPopupView?.Hide();
            if (!IsStepFor(activeStep, _rouletteDisplay))
                _rouletteDisplay?.Dispose();
            if (!IsStepFor(activeStep, _postGamePreviewView))
                _postGamePreviewView?.SetVisible(false);
        }

        private void ShowPreGamePreview()
        {
            if (_preGamePreviewView == null)
            {
                _navigation.Next();
                return;
            }

            var contents = HasConfiguredGame
                ? _gameManager.LootboxEngine.GetCurrentContents()
                : new List<Data.ItemData>();
            _preGamePreviewView.Show(contents);
            _preGamePreviewView.SetPreviousInteractable(_navigation.CanPrevious);
        }

        private void BeginHintGame()
        {
            if (!HasConfiguredGame || _hintView == null)
            {
                Debug.LogWarning("[HintGameOpeningCoordinator] Cannot begin hint game until a game is configured.");
                return;
            }

            _gameManager.HintGameEngine.GetOrAssignHint();
            _hintIsActive = true;
            _hintView.SetVisible(true);
            _hintView.SetPreviousInteractable(_navigation.CanPrevious);
            _hintView.ShowHint(_gameManager.HintGameEngine.GetCurrentHint());
        }

        private void ShowCorrectAnswerPopup()
        {
            if (_answerPopupView == null)
            {
                _navigation.Next();
                return;
            }

            _answerPopupView.Show(_pendingAnswerMessage ?? string.Empty, 3f);
        }

        private void StartReveal()
        {
            if (_rouletteDisplay == null)
            {
                _navigation.Next();
                return;
            }

            if (_revealRoutine == null)
                _revealRoutine = StartCoroutine(OpenLootboxRoutine());
        }

        private void ShowPostGamePreview()
        {
            if (_postGamePreviewView == null)
            {
                _navigation.Next();
                return;
            }

            _postGamePreviewView.Show(_gameManager?.LootboxEngine?.GetCurrentWinners()
                ?? new List<Data.ItemData>());
            _postGamePreviewView.SetPreviousInteractable(_navigation.CanPrevious);
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
            _hintView?.SetVisible(false);
            _gameManager.DiscordAnswerListener?.StopListening();
            _pendingAnswerMessage = _gameManager.Session.Config.settings.FormatCorrectAnswer(username, answer);
            _navigation.Next();
        }

        private IEnumerator OpenLootboxRoutine()
        {
            IReadOnlyList<string> rewardItemIds;
            try
            {
                rewardItemIds = _gameManager.OpenLootboxForReveal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _rouletteDisplay.ShowFailure(Settings?.FormatLootboxOpenFailed()
                    ?? Data.GameSettings.DefaultLootboxOpenFailedFormat);
                _revealRoutine = null;
                yield break;
            }

            yield return StartCoroutine(_rouletteDisplay.Play(rewardItemIds));
            _rouletteDisplay.ShowSummary(_rouletteDisplay.CollectedRewardTitles);
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

        private bool IsStepFor<T>(HintGameNavigationStep step, T view) where T : Component
            => step != null && view != null && step.ActivationTarget == view.gameObject;

        private T FindPhaseView<T>() where T : Component
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
