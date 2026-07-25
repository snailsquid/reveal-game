using System;

namespace RevealGame.Data
{
    /// <summary>
    /// Top-level game configuration deserialized from YAML.
    /// Contains all items, hints, and settings for a game session.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public ItemData[] items;
        public HintData[] hints;
        public GameSettings settings;
    }

    /// <summary>
    /// Game-wide settings from YAML configuration.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public const string DefaultCorrectAnswerFormat = "{user} guessed the correct answer:\n'{answer}'";

        public int nLootbox;
        public string correctAnswerFormat = DefaultCorrectAnswerFormat;

        public string FormatCorrectAnswer(string user, string answer)
        {
            var format = string.IsNullOrEmpty(correctAnswerFormat) ? DefaultCorrectAnswerFormat : correctAnswerFormat;
            return format.Replace("{user}", user ?? "").Replace("{answer}", answer ?? "");
        }
    }
}
