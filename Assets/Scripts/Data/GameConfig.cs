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
        public int nLootbox; // Number of lootboxes
    }
}
