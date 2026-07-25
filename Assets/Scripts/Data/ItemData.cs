using System;
using UnityEngine;
using UnityEngine.Video;

namespace RevealGame.Data
{
    [Serializable]
    public class ItemData
    {
        public string id;
        public string title;
        public string description;
        public string mediaPath;
        public bool isWinner;

        [NonSerialized] public Sprite sprite;
        [NonSerialized] public VideoClip videoClip;

        public bool HasDirectMedia => sprite != null || videoClip != null;
    }
}
