using System;
using UnityEngine;

namespace RevealGame.Data
{
    public enum HintType
    {
        Image = 0,
        Text = 1,
        Audio = 2,
        Video = 3
    }

    [Serializable]
    public class HintData
    {
        public string id;
        public string title;
        public HintType hintType;
        public string answer;
        public bool isCaseSensitive;
        public string[] mediaPaths;

        [NonSerialized] public Sprite answerImage;
        [NonSerialized] public Sprite[] sprites;

        public bool HasDirectMedia => sprites != null && sprites.Length > 0;

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
