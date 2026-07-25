using System.Collections.Generic;

namespace RevealGame.Data
{
    [System.Serializable]
    public class GameConfig
    {
        public ItemData[] items;
        public HintData[] hints;
        public GameSettings settings;
    }

    [System.Serializable]
    public class GameSettings
    {
        public const string DefaultCorrectAnswerFormat = "{user} guessed the correct answer:\n'{answer}'";
        public const string DefaultDefaultHintTitleFormat = "Solve the puzzle!";
        public const string DefaultOpenLootboxButtonLabelFormat = "Open Lootbox";
        public const string DefaultBackToLootboxesButtonLabelFormat = "Back to Lootboxes";
        public const string DefaultBackButtonLabelFormat = "Back";
        public const string DefaultLootboxOpenFailedFormat = "The lootbox could not be opened.";
        public const string DefaultLootboxOpenedNoPrizesFormat = "Lootbox opened\n\nNo prizes this time.";
        public const string DefaultLootboxOpenedWithPrizesFormat = "Lootbox opened\n\nYou unlocked {count} item(s):\n{items}";
        public const string DefaultRewardSpinningFormat = "Reward {index} of {count}\nSpinning...";
        public const string DefaultRewardRevealedFormat = "Reward {index} of {count}\n{title}";
        public const string DefaultLootboxLabelFormat = "Box {index}";
        public const string DefaultOpenedNoPrizeFormat = "Opened\nNo prize";
        public const string DefaultDiscordUnavailableFormat = "Discord server unavailable.";

        public int nLootbox;
        public string correctAnswerFormat = DefaultCorrectAnswerFormat;
        public string defaultHintTitleFormat = DefaultDefaultHintTitleFormat;
        public string openLootboxButtonLabelFormat = DefaultOpenLootboxButtonLabelFormat;
        public string backToLootboxesButtonLabelFormat = DefaultBackToLootboxesButtonLabelFormat;
        public string backButtonLabelFormat = DefaultBackButtonLabelFormat;
        public string lootboxOpenFailedFormat = DefaultLootboxOpenFailedFormat;
        public string lootboxOpenedNoPrizesFormat = DefaultLootboxOpenedNoPrizesFormat;
        public string lootboxOpenedWithPrizesFormat = DefaultLootboxOpenedWithPrizesFormat;
        public string rewardSpinningFormat = DefaultRewardSpinningFormat;
        public string rewardRevealedFormat = DefaultRewardRevealedFormat;
        public string lootboxLabelFormat = DefaultLootboxLabelFormat;
        public string openedNoPrizeFormat = DefaultOpenedNoPrizeFormat;
        public string discordUnavailableFormat = DefaultDiscordUnavailableFormat;

        public string FormatCorrectAnswer(string user, string answer)
        {
            var format = string.IsNullOrEmpty(correctAnswerFormat) ? DefaultCorrectAnswerFormat : correctAnswerFormat;
            return format.Replace("{user}", user ?? "").Replace("{answer}", answer ?? "");
        }

        public string FormatDefaultHintTitle()
        {
            return string.IsNullOrEmpty(defaultHintTitleFormat) ? DefaultDefaultHintTitleFormat : defaultHintTitleFormat;
        }

        public string FormatOpenLootboxButtonLabel()
        {
            return string.IsNullOrEmpty(openLootboxButtonLabelFormat) ? DefaultOpenLootboxButtonLabelFormat : openLootboxButtonLabelFormat;
        }

        public string FormatBackToLootboxesButtonLabel()
        {
            return string.IsNullOrEmpty(backToLootboxesButtonLabelFormat) ? DefaultBackToLootboxesButtonLabelFormat : backToLootboxesButtonLabelFormat;
        }

        public string FormatBackButtonLabel()
        {
            return string.IsNullOrEmpty(backButtonLabelFormat) ? DefaultBackButtonLabelFormat : backButtonLabelFormat;
        }

        public string FormatLootboxOpenFailed()
        {
            return string.IsNullOrEmpty(lootboxOpenFailedFormat) ? DefaultLootboxOpenFailedFormat : lootboxOpenFailedFormat;
        }

        public string FormatLootboxOpenedNoPrizes()
        {
            return string.IsNullOrEmpty(lootboxOpenedNoPrizesFormat) ? DefaultLootboxOpenedNoPrizesFormat : lootboxOpenedNoPrizesFormat;
        }

        public string FormatLootboxOpenedWithPrizes(int count, IReadOnlyList<string> itemTitles)
        {
            var format = string.IsNullOrEmpty(lootboxOpenedWithPrizesFormat) ? DefaultLootboxOpenedWithPrizesFormat : lootboxOpenedWithPrizesFormat;
            return format
                .Replace("{count}", count.ToString())
                .Replace("{items}", string.Join("\n", itemTitles));
        }

        public string FormatRewardSpinning(int index, int count)
        {
            var format = string.IsNullOrEmpty(rewardSpinningFormat) ? DefaultRewardSpinningFormat : rewardSpinningFormat;
            return format.Replace("{index}", index.ToString()).Replace("{count}", count.ToString());
        }

        public string FormatRewardRevealed(int index, int count, string title)
        {
            var format = string.IsNullOrEmpty(rewardRevealedFormat) ? DefaultRewardRevealedFormat : rewardRevealedFormat;
            return format
                .Replace("{index}", index.ToString())
                .Replace("{count}", count.ToString())
                .Replace("{title}", title ?? "");
        }

        public string FormatLootboxLabel(int index)
        {
            var format = string.IsNullOrEmpty(lootboxLabelFormat) ? DefaultLootboxLabelFormat : lootboxLabelFormat;
            return format.Replace("{index}", index.ToString());
        }

        public string FormatOpenedNoPrize()
        {
            return string.IsNullOrEmpty(openedNoPrizeFormat) ? DefaultOpenedNoPrizeFormat : openedNoPrizeFormat;
        }

        public string FormatDiscordUnavailable()
        {
            return string.IsNullOrEmpty(discordUnavailableFormat) ? DefaultDiscordUnavailableFormat : discordUnavailableFormat;
        }
    }
}
