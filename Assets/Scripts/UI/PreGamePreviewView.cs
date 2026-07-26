using UnityEngine;

namespace RevealGame.UI
{
    public sealed class PreGamePreviewView : ItemPreviewView
    {
        protected override Color PanelColor => new Color(0.05f, 0.08f, 0.14f, 0.98f);
        protected override Color AccentColor => new Color(0.4f, 0.9f, 1f);
    }
}
