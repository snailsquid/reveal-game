using System;
using System.Linq;
using RevealGame.Data;
using UnityEngine;

namespace RevealGame.Importing
{
    public static class ScriptableObjectConfigAdapter
    {
        public static GameConfig ToGameConfig(GameConfigSO so)
        {
            if (so == null)
                throw new ArgumentNullException(nameof(so));

            if (!so.Validate(out var error))
                throw new InvalidOperationException($"Invalid GameConfigSO '{so.name}': {error}");

            var config = new GameConfig
            {
                settings = new GameSettings { nLootbox = so.NLootbox, correctAnswerFormat = so.CorrectAnswerFormat },
                items = so.Items.Select((entry, i) => ToItemData(entry, i)).ToArray(),
                hints = so.Hints.Select((entry, i) => ToHintData(entry, i)).ToArray()
            };

            return config;
        }

        private static ItemData ToItemData(GameConfigSO.ItemEntry entry, int index)
        {
            return new ItemData
            {
                id = $"item_{index}",
                title = entry.title,
                description = entry.description,
                isWinner = entry.isWinner,
                sprite = entry.image,
                videoClip = entry.video
            };
        }

        private static HintData ToHintData(GameConfigSO.HintEntry entry, int index)
        {
            return new HintData
            {
                id = $"hint_{index}",
                title = entry.title,
                hintType = entry.hintType,
                answer = entry.answer,
                isCaseSensitive = entry.isCaseSensitive,
                sprites = entry.images
            };
        }
    }
}
