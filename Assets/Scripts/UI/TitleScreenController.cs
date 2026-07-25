using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _playButton;

        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;

        private void Awake()
        {
            if (_playButton == null)
            {
                Debug.LogError("[TitleScreenController] PlayButton not assigned");
                enabled = false;
                return;
            }

            _playButton.onClick.AddListener(OnPlayClicked);
            var label = _playButton.GetComponentInChildren<Text>(true);
            if (label != null) label.color = new Color(0.08f, 0.16f, 0.12f);
        }

        private void OnPlayClicked()
        {
            _gameManager.ShowLootboxScreen();
        }
    }
}
