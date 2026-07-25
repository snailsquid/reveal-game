using System;
using UnityEngine;
using UnityEngine.Video;

namespace RevealGame.Data
{
    [CreateAssetMenu(fileName = "NewGameConfig", menuName = "RevealGame/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [SerializeField] private int _nLootbox = 1;
        [SerializeField] private ItemEntry[] _items = Array.Empty<ItemEntry>();
        [SerializeField] private HintEntry[] _hints = Array.Empty<HintEntry>();

        public int NLootbox => _nLootbox;
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
