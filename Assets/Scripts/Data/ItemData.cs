using System;

namespace RevealGame.Data
{
    /// <summary>
    /// Data model for a single item in a lootbox.
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string id;
        public string title;
        public string description;
        public string mediaPath; // Relative path to image/video inside the ZIP
        public bool isWinner;
    }
}
