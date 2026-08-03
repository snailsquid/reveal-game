using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Creates and controls the persistent music mute button shown over the game screens.
    /// The control is created at runtime so it remains available while screens are swapped.
    /// </summary>
    public sealed class MusicToggleButton : MonoBehaviour
    {
        private const string ObjectName = "MusicToggleButton";

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _musicOnLabel = "Music: On";
        [SerializeField] private string _musicOffLabel = "Music: Muted";

        public static MusicToggleButton EnsureInstalled()
        {
            var existing = FindAnyObjectByType<MusicToggleButton>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[MusicToggleButton] No Canvas found; music toggle was not created");
                return null;
            }

            var root = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();

            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-32f, -32f);
            rect.sizeDelta = new Vector2(180f, 54f);

            var background = root.GetComponent<Image>();
            background.color = new Color(0.08f, 0.11f, 0.16f, 0.94f);
            background.raycastTarget = true;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.color = Color.white;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 22f;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;

            var toggle = root.AddComponent<MusicToggleButton>();
            toggle._button = root.GetComponent<Button>();
            toggle._label = label;
            toggle._button.targetGraphic = background;
            toggle._button.transition = Selectable.Transition.ColorTint;
            toggle._button.colors = new ColorBlock
            {
                normalColor = new Color(0.08f, 0.11f, 0.16f, 0.94f),
                highlightedColor = new Color(0.16f, 0.22f, 0.32f, 0.98f),
                pressedColor = new Color(0.22f, 0.30f, 0.42f, 1f),
                selectedColor = new Color(0.16f, 0.22f, 0.32f, 0.98f),
                disabledColor = new Color(0.08f, 0.11f, 0.16f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            toggle.Initialize();
            ButtonInteractionAnimation.EnsureAttached(toggle._button);
            return toggle;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
        }

        private void Initialize()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_label == null)
                _label = GetComponentInChildren<TMP_Text>(true);
            if (_button == null)
                return;

            _button.onClick.RemoveListener(OnButtonClicked);
            _button.onClick.AddListener(OnButtonClicked);
            UpdateLabel();
        }

        private void OnButtonClicked()
        {
            UiSoundPlayer.ToggleMusicMuted();
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (_label != null)
                _label.text = UiSoundPlayer.IsMusicMuted ? _musicOffLabel : _musicOnLabel;
        }
    }
}
