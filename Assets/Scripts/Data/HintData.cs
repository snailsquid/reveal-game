using System;

namespace RevealGame.Data
{
    /// <summary>
    /// Types of hint puzzles a player can solve.
    /// </summary>
    public enum HintType
    {
        Image = 0,
        Text = 1,
        Audio = 2,
        Video = 3
    }

    /// <summary>
    /// Data model for a hint puzzle.
    /// </summary>
    [Serializable]
    public class HintData
    {
        public string id; // Unique identifier
        public string title; // Display title for the hint
        public HintType hintType; // Type of hint (image, text, audio, video)
        public string answer; // Correct answer (exact match)
        public bool isCaseSensitive; // Whether answer matching is case-sensitive
        public string[] mediaPaths; // Paths to media files (images, videos, audio)

        /// <summary>
        /// Checks if the player's answer is correct.
        /// </summary>
        public bool IsAnswerCorrect(string playerAnswer)
        {
            if (string.IsNullOrEmpty(playerAnswer))
                return false;

            StringComparison comparison = isCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return string.Equals(answer, playerAnswer, comparison);
        }
    }
}
