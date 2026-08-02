using System;
using UnityEngine;
using UnityEngine.Video;

namespace RevealGame.Data
{
    public enum ItemImageAlignment
    {
        Center,
        Left,
        Right
    }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string title;
        public string teamName;
        public string description;
        public string mediaPath;
        public bool isWinner;
        public ItemImageAlignment imageAlignment = ItemImageAlignment.Center;

        [NonSerialized] public Sprite sprite;
        [NonSerialized] public VideoClip videoClip;

        public bool HasDirectMedia => sprite != null || videoClip != null;
    }
}
