using UnityEngine;

namespace RevealGame.UI
{
    public sealed class PostGamePreviewView : ItemPreviewView
    {
        protected override Color PanelColor => new Color(0.08f, 0.12f, 0.08f, 0.98f);
        protected override Color AccentColor => new Color(0.5f, 1f, 0.55f);
    }
}
