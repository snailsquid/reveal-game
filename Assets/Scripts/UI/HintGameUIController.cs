using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RevealGame.UI
{
    public class HintGameUIController : MonoBehaviour
    {
        [Header("Phase Views")]
        [SerializeField] private HintGameView _hintView;
        [SerializeField] private AnswerPopupView _answerPopupView;
        [SerializeField] private RouletteDisplay _rouletteDisplay;

        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;

        private enum OpeningState
        {
            AwaitingStart,
            RevealingRewards,
            AwaitingExit
        }

        private OpeningState _openingState;
        private Coroutine _matchedAnswerRoutine;

        private Data.GameSettings Settings => _gameManager?.Session?.Config?.settings;

        private void Awake()
        {
            if (_hintView != null) _hintView.Setup(_gameManager);
            if (_answerPopupView != null) _answerPopupView.Setup(_gameManager);
            if (_rouletteDisplay != null) _rouletteDisplay.Setup(_gameManager);
        }

        private void OnEnable()
        {
            SubscribeToDiscordListener();
            _openingState = OpeningState.AwaitingStart;

            if (_hintView != null)
            {
                _hintView.AnswerSubmitted += OnHintAnswerSubmitted;
                _hintView.BackRequested += OnHintBackRequested;
                _hintView.SetVisible(true);
            }

            if (_answerPopupView != null)
            {
                _answerPopupView.Dismissed += OnPopupDismissed;
                _answerPopupView.Hide();
            }

            if (_rouletteDisplay != null)
            {
                _rouletteDisplay.ContinueRequested += OnRouletteContinue;
                _rouletteDisplay.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromDiscordListener();
            _gameManager?.DiscordAnswerListener?.StopListening();

            if (_hintView != null)
            {
                _hintView.AnswerSubmitted -= OnHintAnswerSubmitted;
                _hintView.BackRequested -= OnHintBackRequested;
            }

            if (_answerPopupView != null)
                _answerPopupView.Dismissed -= OnPopupDismissed;

            if (_rouletteDisplay != null)
                _rouletteDisplay.ContinueRequested -= OnRouletteContinue;
        }

        public void ShowCurrentHint()
        {
            var hint = _gameManager.HintGameEngine.GetCurrentHint();
            _hintView?.ShowHint(hint);
        }

        private void OnHintAnswerSubmitted(string answer)
        {
            if (_openingState != OpeningState.AwaitingStart)
                return;

            ProcessAnswer(answer, "Player");
        }

        private void OnDiscordAnswerMatched(DiscordAnswerMatch match)
        {
            if (_openingState != OpeningState.AwaitingStart)
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

            if (_hintView != null) _hintView.SetVisible(false);
            _gameManager.DiscordAnswerListener?.StopListening();

            if (_answerPopupView != null)
            {
                // ResultPopup starts inactive, so enable the view component before
                // invoking its presentation lifecycle and dismissal event.
                if (!_answerPopupView.gameObject.activeSelf)
                    _answerPopupView.gameObject.SetActive(true);

                string message = _gameManager.Session.Config.settings.FormatCorrectAnswer(username, answer);
                _answerPopupView.Show(message, 3f);
            }
            else
            {
                _matchedAnswerRoutine = StartCoroutine(ShowMatchThenOpenLootbox());
            }
        }

        private void OnPopupDismissed()
        {
            if (_openingState == OpeningState.AwaitingStart)
                StartCoroutine(OpenLootboxRoutine());
        }

        private void OnRouletteContinue()
        {
            if (_openingState == OpeningState.AwaitingExit)
                _gameManager.CompleteLootboxOpening();
        }

        private void OnHintBackRequested()
        {
            if (_openingState == OpeningState.RevealingRewards)
                return;

            if (_openingState == OpeningState.AwaitingExit)
                _gameManager.CompleteLootboxOpening();
            else
                _gameManager.CancelLootboxOpening();
        }

        private IEnumerator ShowMatchThenOpenLootbox()
        {
            yield return new WaitForSecondsRealtime(3f);
            _matchedAnswerRoutine = null;

            if (_openingState == OpeningState.AwaitingStart)
                StartCoroutine(OpenLootboxRoutine());
        }

        private IEnumerator OpenLootboxRoutine()
        {
            _openingState = OpeningState.RevealingRewards;

            IReadOnlyList<string> rewardItemIds;
            try
            {
                rewardItemIds = _gameManager.OpenLootboxForReveal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _rouletteDisplay?.ShowFailure(Settings?.FormatLootboxOpenFailed()
                    ?? Data.GameSettings.DefaultLootboxOpenFailedFormat);
                _openingState = OpeningState.AwaitingExit;
                yield break;
            }

            if (_answerPopupView != null)
                _answerPopupView.Hide();

            if (_rouletteDisplay != null)
            {
                yield return StartCoroutine(_rouletteDisplay.Play(rewardItemIds));

                var titles = _rouletteDisplay.CollectedRewardTitles;
                _rouletteDisplay.ShowSummary(titles);
            }

            _openingState = OpeningState.AwaitingExit;
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
