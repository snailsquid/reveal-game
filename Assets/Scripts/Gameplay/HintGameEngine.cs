using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RevealGame.Gameplay
{
    /// <summary>
    /// Manages hint game logic: selecting hints, validating answers, tracking completion.
    /// Works with GameSession to maintain state across scenes.
    /// </summary>
    public class HintGameEngine
    {
        private readonly Data.GameSession _session;
        private readonly System.Random _rng;

        public HintGameEngine(Data.GameSession session, System.Random rng = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _rng = rng ?? new System.Random();
        }

        /// <summary>
        /// Gets the current hint for the active lootbox.
        /// </summary>
        public Data.HintData GetCurrentHint()
        {
            return _session.GetCurrentHint();
        }

        /// <summary>
        /// Assigns a new random hint to the current lootbox.
        /// Returns null if all hints have been completed.
        /// </summary>
        public Data.HintData AssignNewHint()
        {
            return _session.AssignNewHint();
        }

        /// <summary>
        /// Gets the active hint, assigning a random one only when the lootbox has none.
        /// </summary>
        public Data.HintData GetOrAssignHint()
        {
            return GetCurrentHint() ?? AssignNewHint();
        }

        /// <summary>
        /// Checks if the player's answer is correct for the current hint.
        /// </summary>
        public bool CheckAnswer(string playerAnswer)
        {
            var hint = GetCurrentHint();
            if (hint == null)
            {
                Debug.LogWarning("[HintGameEngine] No current hint to check answer for");
                return false;
            }

            return hint.IsAnswerCorrect(playerAnswer);
        }

        /// <summary>
        /// Validates and submits the player's answer.
        /// Returns true if correct, false if incorrect.
        /// </summary>
        public AnswerResult SubmitAnswer(string playerAnswer)
        {
            var hint = GetCurrentHint();
            if (hint == null)
            {
                return new AnswerResult
                {
                    IsCorrect = false,
                    Message = "No active hint"
                };
            }

            bool isCorrect = hint.IsAnswerCorrect(playerAnswer);

            if (isCorrect)
            {
                _session.CompleteCurrentHint();
            }

            return new AnswerResult
            {
                IsCorrect = isCorrect,
                Hint = hint,
                Message = isCorrect
                    ? $"Player Answered Correctly\n'{playerAnswer}'"
                    : "Incorrect answer. Try again."
            };
        }

        /// <summary>
        /// Checks if all hints for the current lootbox have been completed.
        /// </summary>
        public bool AreAllHintsCompleted()
        {
            var lootbox = _session.CurrentLootbox;
            if (lootbox == null)
                return false;

            int totalHints = _session.Config.hints?.Length ?? 0;
            return lootbox.AllHintsCompleted(totalHints);
        }

        /// <summary>
        /// Gets the number of hints completed for the current lootbox.
        /// </summary>
        public int GetCompletedHintCount()
        {
            var lootbox = _session.CurrentLootbox;
            return lootbox?.completedHintIds?.Count ?? 0;
        }

        /// <summary>
        /// Gets the total number of hints available.
        /// </summary>
        public int GetTotalHintCount()
        {
            return _session.Config.hints?.Length ?? 0;
        }

        /// <summary>
        /// Resets the hint game for the current lootbox (for testing).
        /// </summary>
        public void ResetForLootbox()
        {
            var lootbox = _session.CurrentLootbox;
            if (lootbox != null)
            {
                lootbox.completedHintIds.Clear();
                lootbox.currentHintId = null;
            }
        }
    }

    /// <summary>
    /// Result of submitting an answer to a hint puzzle.
    /// </summary>
    public class AnswerResult
    {
        public bool IsCorrect;
        public Data.HintData Hint;
        public string Message;
    }
}
