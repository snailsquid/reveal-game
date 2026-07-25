using System;
using UnityEngine;
using UnityEngine.Video;

namespace RevealGame.Data
{
    [CreateAssetMenu(fileName = "NewGameConfig", menuName = "RevealGame/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [SerializeField] private int _nLootbox = 1;
        [SerializeField] private string _correctAnswerFormat = GameSettings.DefaultCorrectAnswerFormat;
        [SerializeField] private string _defaultHintTitleFormat = GameSettings.DefaultDefaultHintTitleFormat;
        [SerializeField] private string _openLootboxButtonLabelFormat = GameSettings.DefaultOpenLootboxButtonLabelFormat;
        [SerializeField] private string _backToLootboxesButtonLabelFormat = GameSettings.DefaultBackToLootboxesButtonLabelFormat;
        [SerializeField] private string _backButtonLabelFormat = GameSettings.DefaultBackButtonLabelFormat;
        [SerializeField] private string _lootboxOpenFailedFormat = GameSettings.DefaultLootboxOpenFailedFormat;
        [SerializeField] private string _lootboxOpenedNoPrizesFormat = GameSettings.DefaultLootboxOpenedNoPrizesFormat;
        [SerializeField] private string _lootboxOpenedWithPrizesFormat = GameSettings.DefaultLootboxOpenedWithPrizesFormat;
        [SerializeField] private string _rewardSpinningFormat = GameSettings.DefaultRewardSpinningFormat;
        [SerializeField] private string _rewardRevealedFormat = GameSettings.DefaultRewardRevealedFormat;
        [SerializeField] private string _lootboxLabelFormat = GameSettings.DefaultLootboxLabelFormat;
        [SerializeField] private string _openedNoPrizeFormat = GameSettings.DefaultOpenedNoPrizeFormat;
        [SerializeField] private string _discordUnavailableFormat = GameSettings.DefaultDiscordUnavailableFormat;
        [SerializeField] private ItemEntry[] _items = Array.Empty<ItemEntry>();
        [SerializeField] private HintEntry[] _hints = Array.Empty<HintEntry>();

        public int NLootbox => _nLootbox;
        public string CorrectAnswerFormat => _correctAnswerFormat;
        public string DefaultHintTitleFormat => _defaultHintTitleFormat;
        public string OpenLootboxButtonLabelFormat => _openLootboxButtonLabelFormat;
        public string BackToLootboxesButtonLabelFormat => _backToLootboxesButtonLabelFormat;
        public string BackButtonLabelFormat => _backButtonLabelFormat;
        public string LootboxOpenFailedFormat => _lootboxOpenFailedFormat;
        public string LootboxOpenedNoPrizesFormat => _lootboxOpenedNoPrizesFormat;
        public string LootboxOpenedWithPrizesFormat => _lootboxOpenedWithPrizesFormat;
        public string RewardSpinningFormat => _rewardSpinningFormat;
        public string RewardRevealedFormat => _rewardRevealedFormat;
        public string LootboxLabelFormat => _lootboxLabelFormat;
        public string OpenedNoPrizeFormat => _openedNoPrizeFormat;
        public string DiscordUnavailableFormat => _discordUnavailableFormat;
        public ItemEntry[] Items => _items;
        public HintEntry[] Hints => _hints;

        [Serializable]
        public class ItemEntry
        {
            public string title;
            public string description;
            public Sprite image;
            public VideoClip video;
            public bool isWinner;
        }

        [Serializable]
        public class HintEntry
        {
            public string title;
            public HintType hintType = HintType.Image;
            public string answer;
            public bool isCaseSensitive;
            public Sprite[] images = Array.Empty<Sprite>();
        }

        public bool Validate(out string error)
        {
            error = null;

            if (_items == null || _items.Length == 0)
            {
                error = "Config must contain at least one item.";
                return false;
            }

            if (_nLootbox <= 0)
            {
                error = "nLootbox must be a positive integer.";
                return false;
            }

            if (_nLootbox > _items.Length)
            {
                error = $"nLootbox ({_nLootbox}) cannot exceed the number of items ({_items.Length}).";
                return false;
            }

            if (_hints == null || _hints.Length == 0)
            {
                error = "Config must contain at least one hint.";
                return false;
            }

            for (var i = 0; i < _hints.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_hints[i].answer))
                {
                    error = $"Hint {i + 1} must have a non-empty answer.";
                    return false;
                }
            }

            return true;
        }
    }
}
